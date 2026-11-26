using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface IRecordsService
{
    Task<EmployeeRecords> GetAsync(string employeeId);
}
