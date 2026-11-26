using ITG.Constants;
using ITG.Data;
using ITG.Models.Entities;
using ITG.Models.Enums;
using ITG.Services.Interfaces;
using ITG.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITG.Areas.Admin.Controllers;

public class EmployeesController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly UserManager<Employee> _userManager;
    private readonly ICareerService _careerService;
    private readonly ISkillAssessmentService _assessmentService;
    private readonly IPromotionService _promotionService;
    private readonly IReadinessService _readinessService;
    private readonly INotificationService _notifications;

    public EmployeesController(
        AppDbContext context,
        UserManager<Employee> userManager,
        ICareerService careerService,
        ISkillAssessmentService assessmentService,
        IPromotionService promotionService,
        IReadinessService readinessService,
        INotificationService notifications)
    {
        _readinessService = readinessService;
        _notifications = notifications;
        _context = context;
        _userManager = userManager;
        _careerService = careerService;
        _assessmentService = assessmentService;
        _promotionService = promotionService;
    }

    // ---------------- Manage list ----------------
    public async Task<IActionResult> Index()
    {
        var employeeRoleId = await _context.Roles
            .Where(r => r.Name == AppRoles.Employee)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => _context.UserRoles.Any(ur => ur.UserId == e.Id && ur.RoleId == employeeRoleId))
            .OrderBy(e => e.EmployeeNumber)
            .Select(e => new EmployeeListItem(
                e.Id, e.Name, e.Email ?? "", e.EmployeeNumber,
                e.DepartmentId, e.Department != null ? e.Department.Name : null,
                e.CurrentPositionId, e.CurrentPosition != null ? e.CurrentPosition.Title : null))
            .ToListAsync();

        await LoadLookupsAsync();
        return View(employees);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EmployeeForm form)
    {
        var isNew = string.IsNullOrEmpty(form.Id);
        if (isNew && string.IsNullOrWhiteSpace(form.Password))
        {
            ModelState.AddModelError(nameof(form.Password), Localizer["Password is required."]);
        }

        if (!ModelState.IsValid)
        {
            ShowFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        var number = form.EmployeeNumber.Trim();
        var numberTaken = await _context.Employees.AnyAsync(e => e.EmployeeNumber == number && e.Id != form.Id);
        if (numberTaken)
        {
            ShowError("This employee ID is already used.");
            return RedirectToAction(nameof(Index));
        }

        IdentityResult result;
        if (isNew)
        {
            var employee = new Employee
            {
                Name = form.Name.Trim(),
                Email = form.Email.Trim(),
                UserName = form.Email.Trim(),
                EmailConfirmed = true,
                EmployeeNumber = number,
                DepartmentId = form.DepartmentId,
                CurrentPositionId = form.CurrentPositionId
            };

            result = await _userManager.CreateAsync(employee, form.Password!);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(employee, AppRoles.Employee);
            }
        }
        else
        {
            var employee = await _userManager.FindByIdAsync(form.Id!);
            if (employee == null)
            {
                return NotFound();
            }

            // A new position means a new roadmap, so the fork choice is cleared
            if (employee.CurrentPositionId != form.CurrentPositionId)
            {
                employee.SelectedCareerPathId = null;
                await _context.EmployeeCareerPathOptions.Where(o => o.EmployeeId == employee.Id).ExecuteDeleteAsync();
            }

            employee.Name = form.Name.Trim();
            employee.Email = form.Email.Trim();
            employee.UserName = form.Email.Trim();
            employee.EmployeeNumber = number;
            employee.DepartmentId = form.DepartmentId;
            employee.CurrentPositionId = form.CurrentPositionId;

            result = await _userManager.UpdateAsync(employee);
        }

        if (result.Succeeded)
        {
            ShowSuccess(isNew ? "The employee was added." : "The employee was updated.");
        }
        else
        {
            // Identity errors (duplicate email, weak password...) are shown as they are
            TempData["StatusError"] = string.Join(" ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------------- Employee record ----------------
    public async Task<IActionResult> Details(string id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Email,
                e.EmployeeNumber,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.CanRetakeSkillsAssessment
            })
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            return NotFound();
        }

        var submissions = await _context.EmployeeCertificates
            .AsNoTracking()
            .Where(s => s.EmployeeId == id)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionRow(s.Id, s.Certificate.Title, s.Status, s.SubmittedAt,
                s.IssuedOn, s.ReviewedAt, s.RejectionReason))
            .ToListAsync();

        var attempts = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.EmployeeId == id && a.Status == ExamAttemptStatus.Completed)
            .OrderByDescending(a => a.CompletedAt)
            .Select(a => new ExamAttemptRow(a.Exam.Title, a.AttemptNumber, a.Score, a.CompletedAt))
            .ToListAsync();

        var promotions = await _context.Promotions
            .AsNoTracking()
            .Where(p => p.EmployeeId == id)
            .OrderByDescending(p => p.PromotedAt)
            .Select(p => new PromotionRow(p.FromPosition != null ? p.FromPosition.Title : null, p.ToPosition.Title, p.PromotedAt))
            .ToListAsync();

        var model = new EmployeeDetailsViewModel
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email ?? "",
            EmployeeNumber = employee.EmployeeNumber,
            DepartmentName = employee.DepartmentName,
            Progress = await _careerService.GetProgressAsync(id),
            Readiness = await _readinessService.GetAsync(id),
            LatestAssessment = await _assessmentService.GetLatestAsync(id),
            PreviousAssessment = await _assessmentService.GetPreviousAsync(id),
            CanRetakeAssessment = employee.CanRetakeSkillsAssessment,
            Submissions = submissions,
            ExamAttempts = attempts,
            Promotions = promotions
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Promote(string id)
    {
        var result = await _promotionService.PromoteAsync(id, CurrentUserId);

        if (result.Succeeded)
        {
            ShowSuccess("The employee was promoted successfully.");
        }
        else
        {
            ShowError(result.ErrorKey!);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // At a fork: HR assigns one path, or opens several for the employee to choose from
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCareerPath(string id, int[]? careerPathIds)
    {
        var result = await _careerService.SetPathOptionsAsync(id, careerPathIds ?? Array.Empty<int>());

        if (result.Succeeded)
        {
            ShowSuccess(careerPathIds!.Distinct().Count() == 1
                ? "The career path was assigned to the employee."
                : "The paths were opened. The employee can now choose one of them.");
        }
        else
        {
            ShowError(result.ErrorKey!);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // Allows the employee to take the soft-skills assessment again
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockAssessment(string id)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
        {
            return NotFound();
        }

        employee.CanRetakeSkillsAssessment = true;
        await _context.SaveChangesAsync();

        await _notifications.NotifyAsync(id, "HR re-opened your skills assessment. You can take it again.",
            "bi-unlock", "/SkillsAssessment");

        ShowSuccess("The skills assessment was re-opened for this employee.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------------- Helpers ----------------
    private async Task LoadLookupsAsync()
    {
        ViewBag.Departments = await _context.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new LookupItem(d.Id, d.Name))
            .ToListAsync();

        ViewBag.Positions = await _context.Positions
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.CareerPath.Name).ThenBy(p => p.Level)
            .Select(p => new LookupItem(p.Id, p.Title + " · " + p.CareerPath.Name))
            .ToListAsync();
    }
}
