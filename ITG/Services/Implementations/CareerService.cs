using ITG.Data;
using ITG.Models.Entities;
using ITG.Models.Enums;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

// Calculates where each employee is in their career path.
// Positions, paths and roadmaps are small tables, so they are loaded once (the "catalog")
// and the calculation is done in memory. This keeps the admin dashboard fast.
public class CareerService : ICareerService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notifications;

    public CareerService(AppDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<CareerProgress> GetProgressAsync(string employeeId)
    {
        var result = await GetProgressForEmployeesAsync(new[] { employeeId });
        return result.TryGetValue(employeeId, out var progress) ? progress : new CareerProgress();
    }

    public async Task<IReadOnlyDictionary<string, CareerProgress>> GetProgressForEmployeesAsync(
        IReadOnlyCollection<string> employeeIds)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.CurrentPositionId, e.SelectedCareerPathId })
            .ToListAsync();

        var catalog = await LoadCatalogAsync();

        var submissions = await _context.EmployeeCertificates
            .AsNoTracking()
            .Where(s => employeeIds.Contains(s.EmployeeId))
            .Select(s => new Submission(s.Id, s.EmployeeId, s.CertificateId, s.Status, s.SubmittedAt, s.IssuedOn, s.RejectionReason))
            .ToListAsync();

        var submissionsByEmployee = submissions.ToLookup(s => s.EmployeeId);

        // Paths HR opened for employees standing at a fork
        var openedPaths = (await _context.EmployeeCareerPathOptions
                .AsNoTracking()
                .Where(o => employeeIds.Contains(o.EmployeeId))
                .Select(o => new { o.EmployeeId, o.CareerPathId })
                .ToListAsync())
            .ToLookup(o => o.EmployeeId, o => o.CareerPathId);

        return employees.ToDictionary(
            e => e.Id,
            e => Calculate(e.CurrentPositionId, e.SelectedCareerPathId, openedPaths[e.Id].ToHashSet(),
                catalog, submissionsByEmployee[e.Id].ToList()));
    }

    // The employee picks one of the paths HR opened. The choice is final (only HR can change it).
    public async Task<bool> SelectPathAsync(string employeeId, int careerPathId)
    {
        var progress = await GetProgressAsync(employeeId);
        if (!progress.CanChoosePath || progress.PathOptions.All(o => o.CareerPathId != careerPathId))
        {
            return false;
        }

        var employee = await _context.Employees
            .Include(e => e.CareerPathOptions)
            .FirstAsync(e => e.Id == employeeId);

        // Achievements are linked to the employee and certificate (not the path),
        // so nothing is lost when the path changes.
        employee.SelectedCareerPathId = careerPathId;
        employee.CareerPathOptions.Clear();
        await _context.SaveChangesAsync();

        var pathName = progress.PathOptions.First(o => o.CareerPathId == careerPathId).PathName;
        await _notifications.NotifyAdminsAsync("{0} chose the {1} career path.", "bi-check2-circle",
            $"/Admin/Employees/Details/{employeeId}", employee.Name, pathName);
        return true;
    }

    // HR decides the path at a fork: one path = assigned directly,
    // several paths = the employee chooses one of them.
    public async Task<ServiceResult> SetPathOptionsAsync(string employeeId, IReadOnlyCollection<int> careerPathIds)
    {
        var progress = await GetProgressAsync(employeeId);
        if (!progress.IsAtFork)
        {
            return ServiceResult.Fail("No career path decision is needed for this employee right now.");
        }

        var chosen = careerPathIds.Distinct().ToList();
        if (chosen.Count == 0)
        {
            return ServiceResult.Fail("Choose at least one path.");
        }

        if (chosen.Any(id => progress.AllPathOptions.All(o => o.CareerPathId != id)))
        {
            return ServiceResult.Fail("This path cannot be selected.");
        }

        var employee = await _context.Employees
            .Include(e => e.CareerPathOptions)
            .FirstAsync(e => e.Id == employeeId);

        // One path = assigned (no open options); several = opened for the employee to choose
        var keep = chosen.Count == 1 ? new List<int>() : chosen;
        employee.SelectedCareerPathId = chosen.Count == 1 ? chosen[0] : null;

        foreach (var option in employee.CareerPathOptions.Where(o => !keep.Contains(o.CareerPathId)).ToList())
        {
            employee.CareerPathOptions.Remove(option);
        }
        foreach (var pathId in keep.Where(id => employee.CareerPathOptions.All(o => o.CareerPathId != id)))
        {
            employee.CareerPathOptions.Add(new EmployeeCareerPathOption { EmployeeId = employeeId, CareerPathId = pathId });
        }

        await _context.SaveChangesAsync();

        if (chosen.Count == 1)
        {
            var option = progress.AllPathOptions.First(o => o.CareerPathId == chosen[0]);
            await _notifications.NotifyAsync(employeeId, "HR set your next career path: {0}.", "bi-signpost-split",
                "/Roadmap", $"{option.PositionTitle} · {option.PathName}");
        }
        else
        {
            await _notifications.NotifyAsync(employeeId, "HR opened {0} career paths for you. Choose the one you prefer.",
                "bi-signpost-split", "/Home", chosen.Count.ToString());
        }

        return ServiceResult.Ok();
    }

    // ---------------- Calculation ----------------

    private static CareerProgress Calculate(int? currentPositionId, int? selectedPathId, HashSet<int> openedPathIds,
        Catalog catalog, List<Submission> submissions)
    {
        if (currentPositionId == null || !catalog.Positions.TryGetValue(currentPositionId.Value, out var current))
        {
            return new CareerProgress();
        }

        var allOptions = new List<PathOption>();
        CatalogPosition? target = null;
        var isAtFork = false;

        if (current.IsFork)
        {
            allOptions = GetForkOptions(current, catalog);
            isAtFork = allOptions.Count > 0;

            var chosen = allOptions.FirstOrDefault(o => o.CareerPathId == selectedPathId);
            if (chosen != null)
            {
                target = catalog.Positions[chosen.PositionId];
            }
        }

        if (!isAtFork)
        {
            target = GetNextInPath(current, catalog);
        }

        var items = target == null
            ? new List<RoadmapItem>()
            : catalog.Requirements[target.Id]
                .OrderBy(r => r.SortOrder)
                .Select(r => BuildItem(r, submissions))
                .ToList();

        return new CareerProgress
        {
            CurrentPositionId = current.Id,
            CurrentPositionTitle = current.Title,
            CurrentPathName = catalog.Paths.TryGetValue(current.CareerPathId, out var path) ? path.Name : null,
            IsAtFork = isAtFork,
            AllPathOptions = allOptions,
            // The employee only sees the paths HR opened
            PathOptions = allOptions.Where(o => openedPathIds.Contains(o.CareerPathId)).ToList(),
            SelectedCareerPathId = selectedPathId,
            TargetPositionId = target?.Id,
            TargetPositionTitle = target?.Title,
            Items = items
        };
    }

    private static RoadmapItem BuildItem(CatalogRequirement requirement, List<Submission> submissions)
    {
        var forThis = submissions
            .Where(s => s.CertificateId == requirement.CertificateId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToList();

        // A verified upload always counts (even from an older roadmap);
        // otherwise the latest upload decides the status.
        var chosen = forThis.FirstOrDefault(s => s.Status == VerificationStatus.Verified) ?? forThis.FirstOrDefault();

        var status = chosen == null
            ? RoadmapItemStatus.NotSubmitted
            : chosen.Status switch
            {
                VerificationStatus.Verified => RoadmapItemStatus.Verified,
                VerificationStatus.Rejected => RoadmapItemStatus.Rejected,
                _ => RoadmapItemStatus.Pending
            };

        return new RoadmapItem(
            requirement.CertificateId, requirement.Title, requirement.Description, requirement.Category,
            requirement.SortOrder, status, chosen?.Id, chosen?.SubmittedAt, chosen?.IssuedOn, chosen?.RejectionReason);
    }

    private static CatalogPosition? GetNextInPath(CatalogPosition current, Catalog catalog) =>
        catalog.Positions.Values
            .Where(p => p.CareerPathId == current.CareerPathId && p.Level > current.Level && p.IsActive)
            .OrderBy(p => p.Level)
            .FirstOrDefault();

    // At a fork the employee can continue in the same path (if it has a next level)
    // or move to the first position of any child path.
    private static List<PathOption> GetForkOptions(CatalogPosition current, Catalog catalog)
    {
        var options = new List<PathOption>();

        var sameNext = GetNextInPath(current, catalog);
        if (sameNext != null && catalog.Paths.TryGetValue(current.CareerPathId, out var currentPath))
        {
            options.Add(new PathOption(current.CareerPathId, currentPath.Name, sameNext.Id, sameNext.Title));
        }

        var children = catalog.Paths.Values
            .Where(c => c.ParentId == current.CareerPathId && c.IsActive)
            .OrderBy(c => c.Name);

        foreach (var child in children)
        {
            var first = catalog.Positions.Values
                .Where(p => p.CareerPathId == child.Id && p.IsActive)
                .OrderBy(p => p.Level)
                .FirstOrDefault();

            if (first != null)
            {
                options.Add(new PathOption(child.Id, child.Name, first.Id, first.Title));
            }
        }

        return options;
    }

    // ---------------- Catalog ----------------

    private async Task<Catalog> LoadCatalogAsync()
    {
        var positions = await _context.Positions
            .AsNoTracking()
            .Select(p => new CatalogPosition(p.Id, p.Title, p.Level, p.IsFork, p.IsActive, p.CareerPathId))
            .ToListAsync();

        var paths = await _context.CareerPaths
            .AsNoTracking()
            .Select(c => new CatalogPath(c.Id, c.Name, c.ParentCareerPathId, c.IsActive))
            .ToListAsync();

        var requirements = await _context.PositionCertificates
            .AsNoTracking()
            .Where(pc => pc.Certificate.IsActive)
            .Select(pc => new CatalogRequirement(pc.PositionId, pc.CertificateId, pc.Certificate.Title,
                pc.Certificate.Description, pc.Certificate.Category, pc.SortOrder))
            .ToListAsync();

        return new Catalog(
            positions.ToDictionary(p => p.Id),
            paths.ToDictionary(p => p.Id),
            requirements.ToLookup(r => r.PositionId));
    }

    private sealed record CatalogPosition(int Id, string Title, int Level, bool IsFork, bool IsActive, int CareerPathId);
    private sealed record CatalogPath(int Id, string Name, int? ParentId, bool IsActive);
    private sealed record CatalogRequirement(int PositionId, int CertificateId, string Title, string? Description, string Category, int SortOrder);
    private sealed record Submission(int Id, string EmployeeId, int CertificateId, VerificationStatus Status,
        DateTime SubmittedAt, DateTime? IssuedOn, string? RejectionReason);

    private sealed record Catalog(
        Dictionary<int, CatalogPosition> Positions,
        Dictionary<int, CatalogPath> Paths,
        ILookup<int, CatalogRequirement> Requirements);
}
