namespace ITG.Services.Models;

// How close an employee is to the next promotion (0-100).
// Certificates are the official promotion rule, so they weigh the most.
public record PromotionReadiness
{
    public const int CertificatesWeight = 60;
    public const int ExamsWeight = 20;
    public const int SkillsWeight = 20;

    // False when there is no next role yet (top of the path, waiting for HR, no position)
    public bool Applies { get; init; }

    public int Score { get; init; }

    // The three parts, each out of 100
    public int CertificatesPercent { get; init; }
    public int ExamsPercent { get; init; }
    public int SkillsPercent { get; init; }

    // False when no exam is linked to the next role (its weight goes to the certificates)
    public bool HasNextRoleExams { get; init; }
    public bool HasAssessment { get; init; }

    // Certificates still not verified
    public int RemainingCertificates { get; init; }

    // All certificates verified: only HR's promotion is left
    public bool RequirementsComplete { get; init; }

    // Expected days until the requirements are complete, from the employee's own pace
    // (null = not enough history to estimate)
    public int? EstimatedDays { get; init; }

    public static PromotionReadiness NotApplicable { get; } = new() { Applies = false };
}
