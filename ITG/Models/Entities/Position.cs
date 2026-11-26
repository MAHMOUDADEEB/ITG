namespace ITG.Models.Entities;

public class Position
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // True when, after this position, the employee chooses between several career paths
    public bool IsFork { get; set; }

    public int CareerPathId { get; set; }
    public CareerPath CareerPath { get; set; } = null!;

    // Certificates required to REACH this position (its roadmap)
    public ICollection<PositionCertificate> PositionCertificates { get; set; } = new List<PositionCertificate>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    // Exams shown to employees in (or working towards) this position
    public ICollection<ExamPosition> ExamPositions { get; set; } = new List<ExamPosition>();
}
