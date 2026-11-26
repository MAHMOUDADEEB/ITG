using ITG.Data;
using ITG.Models.Entities;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

public class PromotionService : IPromotionService
{
    private readonly AppDbContext _context;
    private readonly ICareerService _careerService;
    private readonly INotificationService _notifications;

    public PromotionService(AppDbContext context, ICareerService careerService, INotificationService notifications)
    {
        _context = context;
        _careerService = careerService;
        _notifications = notifications;
    }

    public async Task<ServiceResult> PromoteAsync(string employeeId, string adminId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee == null)
        {
            return ServiceResult.Fail("Employee not found.");
        }

        // The rule is checked again on the server, not only by hiding the button
        var progress = await _careerService.GetProgressAsync(employeeId);
        if (!progress.IsReadyForPromotion || progress.TargetPositionId == null)
        {
            return ServiceResult.Fail("This employee has not completed the roadmap yet.");
        }

        _context.Promotions.Add(new Promotion
        {
            EmployeeId = employee.Id,
            FromPositionId = employee.CurrentPositionId,
            ToPositionId = progress.TargetPositionId.Value,
            PromotedById = adminId,
            PromotedAt = DateTime.UtcNow,
            Note = "Completed all required certificates."
        });

        // New position, so a new roadmap starts (fork choice is cleared)
        employee.CurrentPositionId = progress.TargetPositionId.Value;
        employee.SelectedCareerPathId = null;

        await _context.SaveChangesAsync();

        // Paths HR opened at the old fork no longer apply
        await _context.EmployeeCareerPathOptions.Where(o => o.EmployeeId == employee.Id).ExecuteDeleteAsync();

        await _notifications.NotifyAsync(employee.Id, "Congratulations! You were promoted to {0}.", "bi-trophy",
            "/Home", progress.TargetPositionTitle ?? "");

        // The new position ends a career stage: HR has to set the next path
        var next = await _careerService.GetProgressAsync(employee.Id);
        if (next.IsAwaitingPathDecision)
        {
            await _notifications.NotifyAdminsAsync("{0} completed the current career stage. Please set the next career path.",
                "bi-signpost-split", $"/Admin/Employees/Details/{employee.Id}", employee.Name);
        }

        return ServiceResult.Ok();
    }
}
