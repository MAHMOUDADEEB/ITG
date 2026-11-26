using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface IPromotionService
{
    // Moves an employee whose roadmap is 100% complete to the target position
    Task<ServiceResult> PromoteAsync(string employeeId, string adminId);
}
