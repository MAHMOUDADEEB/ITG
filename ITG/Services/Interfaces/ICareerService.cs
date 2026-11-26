using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface ICareerService
{
    // Current role, target role (or fork options) and roadmap status of an employee
    Task<CareerProgress> GetProgressAsync(string employeeId);

    // Same as above for many employees at once (admin dashboard), using a few queries only
    Task<IReadOnlyDictionary<string, CareerProgress>> GetProgressForEmployeesAsync(IReadOnlyCollection<string> employeeIds);

    // Saves the path the employee chose at a fork (from the paths HR opened). Returns false if not allowed.
    Task<bool> SelectPathAsync(string employeeId, int careerPathId);

    // HR decides the path at a fork: one path = assigned, several = the employee chooses
    Task<ServiceResult> SetPathOptionsAsync(string employeeId, IReadOnlyCollection<int> careerPathIds);
}
