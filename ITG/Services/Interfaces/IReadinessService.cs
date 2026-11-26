using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface IReadinessService
{
    Task<PromotionReadiness> GetAsync(string employeeId);

    // Many employees at once (admin analytics), with a few database queries
    Task<IReadOnlyDictionary<string, PromotionReadiness>> GetForEmployeesAsync(IReadOnlyCollection<string> employeeIds);
}
