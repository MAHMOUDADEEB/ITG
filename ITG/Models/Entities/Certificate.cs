namespace ITG.Models.Entities;

// A certificate the company recognises (e.g. "SQL Fundamentals").
// Employees earn it outside the website and upload proof (PDF or image).
public class Certificate
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PositionCertificate> PositionCertificates { get; set; } = new List<PositionCertificate>();
    public ICollection<EmployeeCertificate> Submissions { get; set; } = new List<EmployeeCertificate>();
}
