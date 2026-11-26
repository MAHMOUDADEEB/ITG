using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface IDevelopmentPlanService
{
    // The employee's latest plan (null if none was created yet)
    Task<PlanView?> GetCurrentAsync(string employeeId);

    // Asks the AI for a new 4-week plan (replaces the old one)
    Task<ServiceResult> GenerateAsync(string employeeId, string cultureName);

    // Ticks / unticks one task. Null if the task does not exist.
    Task<PlanTaskResult?> SetTaskDoneAsync(string employeeId, int planId, int week, int task, bool done);
}
