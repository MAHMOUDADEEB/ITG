namespace ITG.Services.Models;

// Everything shown on the admin "Analytics" page (for all employees or one department)
public class AnalyticsData
{
    public int EmployeeCount { get; init; }

    // ----- Key numbers -----
    public int? AverageSkills { get; init; }          // average latest soft-skills score
    public int? ExamPassRate { get; init; }           // % of completed exam attempts with 60+
    public int PromotionsLast6Months { get; init; }
    public int? AverageReadiness { get; init; }       // average promotion readiness
    public int ReadyForPromotion { get; init; }

    // ----- Soft skills: Teamwork, Problem Solving, Communication, Leadership -----
    public int[]? CompanySkills { get; init; }
    public int[]? DepartmentSkills { get; init; }     // only when a department is selected
    public string? DepartmentName { get; init; }

    public IReadOnlyList<ExamStat> Exams { get; init; } = Array.Empty<ExamStat>();
    public IReadOnlyList<MonthCount> PromotionsByMonth { get; init; } = Array.Empty<MonthCount>();
    public IReadOnlyList<StatusCount> Statuses { get; init; } = Array.Empty<StatusCount>();
    public IReadOnlyList<StuckCertificate> StuckCertificates { get; init; } = Array.Empty<StuckCertificate>();
    public IReadOnlyList<NearPromotion> NearPromotion { get; init; } = Array.Empty<NearPromotion>();
}

public record ExamStat(string Title, int Attempts, int PassRate, int AverageScore);

public record MonthCount(DateTime Month, int Count);

public record StatusCount(CareerStatus Status, int Count);

// A certificate that is the next step for many employees (where people are "stuck")
public record StuckCertificate(string Title, int Employees, int Rejected);

public record NearPromotion(string EmployeeId, string Name, string? Department, string? CurrentRole,
    string? TargetRole, PromotionReadiness Readiness);

// Short analysis written by the AI for HR
public class AnalyticsInsight
{
    public string Headline { get; set; } = string.Empty;
    public List<string> Points { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}
