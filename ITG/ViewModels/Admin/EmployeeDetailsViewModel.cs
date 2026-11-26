using ITG.Models.Entities;
using ITG.Models.Enums;
using ITG.Services.Models;

namespace ITG.ViewModels.Admin;

public class EmployeeDetailsViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string EmployeeNumber { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }

    public CareerProgress Progress { get; init; } = new();
    public PromotionReadiness Readiness { get; init; } = PromotionReadiness.NotApplicable;

    public SkillAssessment? LatestAssessment { get; init; }
    public SkillAssessment? PreviousAssessment { get; init; }
    public bool CanRetakeAssessment { get; init; }

    public IReadOnlyList<SubmissionRow> Submissions { get; init; } = Array.Empty<SubmissionRow>();
    public IReadOnlyList<ExamAttemptRow> ExamAttempts { get; init; } = Array.Empty<ExamAttemptRow>();
    public IReadOnlyList<PromotionRow> Promotions { get; init; } = Array.Empty<PromotionRow>();

    public string Initials => string.Concat(Name
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));
}

public record SubmissionRow(
    int Id,
    string CertificateTitle,
    VerificationStatus Status,
    DateTime SubmittedAt,
    DateTime? IssuedOn,
    DateTime? ReviewedAt,
    string? RejectionReason);

public record ExamAttemptRow(string ExamTitle, int AttemptNumber, int? Score, DateTime? CompletedAt);

public record PromotionRow(string? FromPosition, string ToPosition, DateTime PromotedAt);
