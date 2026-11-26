namespace ITG.Models.Entities;

// A company department (Development, Finance, ...). Used to group and filter employees.
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
