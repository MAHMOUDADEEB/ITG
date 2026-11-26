namespace ITG.Models.Entities;

// Result of the AI soft-skills assessment (taken once; admin can re-open it)
public class SkillAssessment
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    // All scores are out of 100. OverallScore is the average of the four skills.
    public int OverallScore { get; set; }
    public int TeamworkScore { get; set; }
    public int ProblemSolvingScore { get; set; }
    public int CommunicationScore { get; set; }
    public int LeadershipScore { get; set; }

    // Short feedback written by the AI
    public string? Summary { get; set; }

    // Questions and the employee's answers (JSON), so the admin can review them
    public string? DetailsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
