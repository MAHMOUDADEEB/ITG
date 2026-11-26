namespace ITG.Models.Entities;

// Personal 4-week development plan written by the AI.
// Only the latest plan of an employee is shown; creating a new one replaces it.
public class DevelopmentPlan
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    // The plan itself (summary, focus areas, weeks and tasks) as JSON
    public string ContentJson { get; set; } = string.Empty;

    // Language the AI wrote the plan in ("en" / "ar")
    public string Language { get; set; } = "en";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
