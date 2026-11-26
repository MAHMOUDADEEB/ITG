using ITG.Constants;
using ITG.Data;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using ITG.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ITG.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly ICareerService _careerService;
    private readonly ICertificateReviewService _reviewService;

    public DashboardController(AppDbContext context, ICareerService careerService, ICertificateReviewService reviewService)
    {
        _context = context;
        _careerService = careerService;
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index(string? q, int? department, CareerStatus? status, int page = 1, int pageSize = 6)
    {
        if (!AdminDashboardViewModel.PageSizeOptions.Contains(pageSize))
        {
            pageSize = AdminDashboardViewModel.PageSizeOptions[0];
        }

        // Only accounts with the Employee role (the admin is not listed)
        var employeeRoleId = await _context.Roles
            .Where(r => r.Name == AppRoles.Employee)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var employees = _context.Employees
            .AsNoTracking()
            .Where(e => _context.UserRoles.Any(ur => ur.UserId == e.Id && ur.RoleId == employeeRoleId));

        // Progress of every employee (needed for the summary cards and the status filter)
        var allIds = await employees.Select(e => e.Id).ToListAsync();
        var progress = await _careerService.GetProgressForEmployeesAsync(allIds);

        // ----- Text and department filters (done by the database) -----
        var filtered = employees;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            filtered = filtered.Where(e =>
                e.Name.Contains(term) || e.EmployeeNumber.Contains(term) || (e.Email != null && e.Email.Contains(term)));
        }

        if (department.HasValue)
        {
            filtered = filtered.Where(e => e.DepartmentId == department);
        }

        var list = await filtered
            .OrderBy(e => e.EmployeeNumber)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.EmployeeNumber,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                CurrentRole = e.CurrentPosition != null ? e.CurrentPosition.Title : null
            })
            .ToListAsync();

        // ----- Status filter (calculated, so done in memory) -----
        var rows = list
            .Select(e =>
            {
                var p = progress.TryGetValue(e.Id, out var found) ? found : new CareerProgress();
                return new EmployeeRow(e.Id, e.Name, e.EmployeeNumber, e.DepartmentName, e.CurrentRole,
                    p.Percent, p.Status, p.PendingCount);
            })
            .Where(r => status == null || r.Status == status)
            .ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(rows.Count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == department))
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalEmployees = allIds.Count,
            ReadyForPromotion = progress.Values.Count(p => p.IsReadyForPromotion),
            PendingReviews = await _reviewService.CountPendingAsync(),
            Search = q,
            DepartmentId = department,
            Status = status,
            Departments = departments,
            Rows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            MatchingCount = rows.Count,
            Page = page,
            PageSize = pageSize
        };

        return View(model);
    }
}
