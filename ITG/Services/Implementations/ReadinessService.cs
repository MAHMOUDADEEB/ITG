using ITG.Data;
using ITG.Models.Enums;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

// Promotion readiness = 60% certificates + 20% next-role exams + 20% soft skills
public class ReadinessService : IReadinessService
{
    // The pace is measured on certificates verified in the last year
    private const int PaceWindowDays = 365;
    private const int MinDaysPerCertificate = 7;

    private readonly AppDbContext _context;
    private readonly ICareerService _careerService;

    public ReadinessService(AppDbContext context, ICareerService careerService)
    {
        _context = context;
        _careerService = careerService;
    }

    public async Task<PromotionReadiness> GetAsync(string employeeId)
    {
        var result = await GetForEmployeesAsync(new[] { employeeId });
        return result.TryGetValue(employeeId, out var readiness) ? readiness : PromotionReadiness.NotApplicable;
    }

    public async Task<IReadOnlyDictionary<string, PromotionReadiness>> GetForEmployeesAsync(
        IReadOnlyCollection<string> employeeIds)
    {
        var progress = await _careerService.GetProgressForEmployeesAsync(employeeIds);

        // Exams linked to each target position
        var targetIds = progress.Values.Where(p => p.TargetPositionId != null)
            .Select(p => p.TargetPositionId!.Value).Distinct().ToList();
        var examsByPosition = (await _context.ExamPositions
                .AsNoTracking()
                .Where(ep => targetIds.Contains(ep.PositionId) && ep.Exam.IsActive)
                .Select(ep => new { ep.PositionId, ep.ExamId })
                .ToListAsync())
            .ToLookup(x => x.PositionId, x => x.ExamId);

        // Best score of every exam each employee completed
        var bestScores = (await _context.ExamAttempts
                .AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId) && a.Status == ExamAttemptStatus.Completed && a.Score != null)
                .GroupBy(a => new { a.EmployeeId, a.ExamId })
                .Select(g => new { g.Key.EmployeeId, g.Key.ExamId, Best = g.Max(a => a.Score!.Value) })
                .ToListAsync())
            .ToLookup(x => x.EmployeeId);

        // Latest soft-skills score
        var latestSkills = (await _context.SkillAssessments
                .AsNoTracking()
                .Where(s => employeeIds.Contains(s.EmployeeId))
                .Select(s => new { s.EmployeeId, s.OverallScore, s.CreatedAt })
                .ToListAsync())
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.CreatedAt).First().OverallScore);

        // Dates the employee's certificates were verified (for the pace)
        var since = DateTime.UtcNow.AddDays(-PaceWindowDays);
        var verifiedDates = (await _context.EmployeeCertificates
                .AsNoTracking()
                .Where(c => employeeIds.Contains(c.EmployeeId) && c.Status == VerificationStatus.Verified
                            && c.ReviewedAt != null && c.ReviewedAt >= since)
                .Select(c => new { c.EmployeeId, ReviewedAt = c.ReviewedAt!.Value })
                .ToListAsync())
            .ToLookup(x => x.EmployeeId, x => x.ReviewedAt);

        var result = new Dictionary<string, PromotionReadiness>();
        foreach (var (employeeId, p) in progress)
        {
            if (!p.HasTarget || p.TotalCount == 0)
            {
                result[employeeId] = PromotionReadiness.NotApplicable;
                continue;
            }

            // ----- Exams of the next role (an exam not taken yet counts as 0) -----
            var examIds = examsByPosition[p.TargetPositionId!.Value].Distinct().ToList();
            var scores = bestScores[employeeId].ToDictionary(x => x.ExamId, x => x.Best);
            var examsPercent = examIds.Count == 0
                ? 0
                : (int)Math.Round(examIds.Average(id => scores.TryGetValue(id, out var s) ? s : 0));

            var hasAssessment = latestSkills.TryGetValue(employeeId, out var skills);

            // With no next-role exams, their weight moves to the certificates
            var certificatesWeight = PromotionReadiness.CertificatesWeight + (examIds.Count == 0 ? PromotionReadiness.ExamsWeight : 0);
            var examsWeight = examIds.Count == 0 ? 0 : PromotionReadiness.ExamsWeight;

            var score = (int)Math.Round((p.Percent * certificatesWeight + examsPercent * examsWeight
                                         + (hasAssessment ? skills : 0) * PromotionReadiness.SkillsWeight) / 100.0);

            var remaining = p.TotalCount - p.CompletedCount;

            result[employeeId] = new PromotionReadiness
            {
                Applies = true,
                Score = Math.Clamp(score, 0, 100),
                CertificatesPercent = p.Percent,
                ExamsPercent = examsPercent,
                SkillsPercent = hasAssessment ? skills : 0,
                HasNextRoleExams = examIds.Count > 0,
                HasAssessment = hasAssessment,
                RemainingCertificates = remaining,
                RequirementsComplete = p.IsReadyForPromotion,
                EstimatedDays = remaining == 0 ? 0 : EstimateDays(verifiedDates[employeeId].ToList(), remaining)
            };
        }

        return result;
    }

    // Average days between verified certificates x certificates left.
    // Needs at least two verified certificates to know the pace.
    private static int? EstimateDays(List<DateTime> verified, int remaining)
    {
        if (verified.Count < 2)
        {
            return null;
        }

        var ordered = verified.OrderBy(d => d).ToList();
        var daysPerCertificate = (ordered[^1] - ordered[0]).TotalDays / (ordered.Count - 1);
        daysPerCertificate = Math.Max(daysPerCertificate, MinDaysPerCertificate);

        return (int)Math.Ceiling(daysPerCertificate * remaining);
    }
}
