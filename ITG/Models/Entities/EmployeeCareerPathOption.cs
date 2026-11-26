namespace ITG.Models.Entities;

// At a fork, the career paths HR has opened for the employee to choose from.
// (When HR opens only one path, it is assigned directly in Employee.SelectedCareerPathId instead.)
public class EmployeeCareerPathOption
{
    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    public int CareerPathId { get; set; }
    public CareerPath CareerPath { get; set; } = null!;
}
