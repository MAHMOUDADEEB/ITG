using System.Text;
using ITG.Constants;
using ITG.Data;
using ITG.Models.Enums;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

// Numbers for the admin "Analytics" page and the AI summary for HR
public class AnalyticsService : IAnalyticsService
{
    private const int MonthsBack = 6;
    private const int MaxExams = 8;
    private const int MaxStuck = 5;
    private const int MaxNearPromotion = 6;

    private readonly AppDbContext _context;
    private readonly ICareerService _careerService;
    private readonly IReadinessService _readinessService;
    private readonly IAiClient _ai;

    public AnalyticsService(AppDbContext context, ICareerService careerService, IReadinessService readinessService, IAiClient ai)
    {
        _context = context;
        _careerService = careerService;
        _readinessService = readinessService;
        _ai = ai;
    }

    public async Task<AnalyticsData> GetAsync(int? departmentId)
    {
        // Only accounts with the Employee role (the admin is not counted)
        var employeeRoleId = await _context.Roles
            .Where(r => r.Name == AppRoles.Employee)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var allEmployees = await _context.Employees
            .AsNoTracking()
            .Where(e => _context.UserRoles.Any(ur => ur.UserId == e.Id && ur.RoleId == employeeRoleId))
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.DepartmentId,
                Department = e.Department != null ? e.Department.Name : null,
                CurrentRole = e.CurrentPosition != null ? e.CurrentPosition.Title : null
            })
            .ToListAsync();

        var allIds = allEmployees.Select(e => e.Id).ToList();
        var scope = departmentId.HasValue ? allEmployees.Where(e => e.DepartmentId == departmentId).ToList() : allEmployees;
        var scopeIds = scope.Select(e => e.Id).ToList();

        // ----- Soft skills (latest assessment of each employee) -----
        var assessments = (await _context.SkillAssessments
                .AsNoTracking()
                .Where(s => allIds.Contains(s.EmployeeId))
                .Select(s => new { s.EmployeeId, s.CreatedAt, s.OverallScore, s.TeamworkScore, s.ProblemSolvingScore, s.CommunicationScore, s.LeadershipScore })
                .ToListAsync())
            .GroupBy(s => s.EmployeeId)
            .Select(g => g.OrderByDescending(s => s.CreatedAt).First())
            .ToList();

        int[]? SkillAverages(IEnumerable<string> ids)
        {
            var set = ids.ToHashSet();
            var rows = assessments.Where(a => set.Contains(a.EmployeeId)).ToList();
            return rows.Count == 0
                ? null
                : new[]
                {
                    (int)Math.Round(rows.Average(a => a.TeamworkScore)),
                    (int)Math.Round(rows.Average(a => a.ProblemSolvingScore)),
                    (int)Math.Round(rows.Average(a => a.CommunicationScore)),
                    (int)Math.Round(rows.Average(a => a.LeadershipScore))
                };
        }

        var scopeAssessments = assessments.Where(a => scopeIds.Contains(a.EmployeeId)).ToList();

        // ----- Exams -----
        var attempts = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => scopeIds.Contains(a.EmployeeId) && a.Status == ExamAttemptStatus.Completed && a.Score != null)
            .Select(a => new { a.Exam.Title, Score = a.Score!.Value })
            .ToListAsync();

        var exams = attempts
            .GroupBy(a => a.Title)
            .Select(g => new ExamStat(g.Key, g.Count(),
                (int)Math.Round(g.Count(a => a.Score >= AppConstants.ExamPassingScore) * 100.0 / g.Count()),
                (int)Math.Round(g.Average(a => a.Score))))
            .OrderByDescending(e => e.Attempts).ThenBy(e => e.PassRate)
            .Take(MaxExams)
            .ToList();

        // ----- Promotions per month -----
        var firstMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(MonthsBack - 1));
        var promotionDates = await _context.Promotions
            .AsNoTracking()
            .Where(p => scopeIds.Contains(p.EmployeeId) && p.PromotedAt >= firstMonth)
            .Select(p => p.PromotedAt)
            .ToListAsync();

        var promotionsByMonth = Enumerable.Range(0, MonthsBack)
            .Select(i => firstMonth.AddMonths(i))
            .Select(m => new MonthCount(m, promotionDates.Count(d => d.Year == m.Year && d.Month == m.Month)))
            .ToList();

        // ----- Career status, stuck certificates, readiness -----
        var progress = await _careerService.GetProgressForEmployeesAsync(scopeIds);
        var readiness = await _readinessService.GetForEmployeesAsync(scopeIds);

        var statuses = progress.Values
            .GroupBy(p => p.Status)
            .Select(g => new StatusCount(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .ToList();

        var stuck = progress.Values
            .Where(p => p.HasTarget && p.NextStep != null)
            .GroupBy(p => p.NextStep!.Title)
            .Select(g => new StuckCertificate(g.Key, g.Count(), g.Count(p => p.NextStep!.Status == RoadmapItemStatus.Rejected)))
            .OrderByDescending(s => s.Employees).ThenByDescending(s => s.Rejected)
            .Take(MaxStuck)
            .ToList();

        var applicable = readiness.Where(r => r.Value.Applies).ToList();
        var near = applicable
            .OrderByDescending(r => r.Value.RequirementsComplete).ThenByDescending(r => r.Value.Score)
            .Take(MaxNearPromotion)
            .Select(r =>
            {
                var e = scope.First(x => x.Id == r.Key);
                return new NearPromotion(e.Id, e.Name, e.Department, e.CurrentRole, progress[e.Id].TargetPositionTitle, r.Value);
            })
            .ToList();

        return new AnalyticsData
        {
            EmployeeCount = scope.Count,
            AverageSkills = scopeAssessments.Count == 0 ? null : (int)Math.Round(scopeAssessments.Average(a => a.OverallScore)),
            ExamPassRate = attempts.Count == 0 ? null
                : (int)Math.Round(attempts.Count(a => a.Score >= AppConstants.ExamPassingScore) * 100.0 / attempts.Count),
            PromotionsLast6Months = promotionDates.Count,
            AverageReadiness = applicable.Count == 0 ? null : (int)Math.Round(applicable.Average(r => r.Value.Score)),
            ReadyForPromotion = progress.Values.Count(p => p.IsReadyForPromotion),
            CompanySkills = SkillAverages(allIds),
            DepartmentSkills = departmentId.HasValue ? SkillAverages(scopeIds) : null,
            DepartmentName = departmentId.HasValue
                ? await _context.Departments.Where(d => d.Id == departmentId).Select(d => d.Name).FirstOrDefaultAsync()
                : null,
            Exams = exams,
            PromotionsByMonth = promotionsByMonth,
            Statuses = statuses,
            StuckCertificates = stuck,
            NearPromotion = near
        };
    }

    public async Task<AnalyticsInsight> WriteInsightAsync(AnalyticsData data, string cultureName)
    {
        var language = AiJson.LanguageName(cultureName);
        var system = $$"""
            You are an HR analytics advisor at ITG (a technology company). Read the statistics of the
            employee development platform and write a short, practical analysis for HR managers in {{language}}.
            Use only the numbers given; do not invent data, names or policies.
            Answer with JSON only: {"headline":"one sentence","points":["3 or 4 short findings"],"recommendation":"one or two sentences"}
            """;

        var answer = await _ai.GenerateAsync(system, new[] { new AiMessage(true, Describe(data)) }, json: true);
        var insight = AiJson.Parse<AnalyticsInsight>(answer);

        return new AnalyticsInsight
        {
            Headline = Cut(insight.Headline, 300),
            Points = (insight.Points ?? new()).Where(p => !string.IsNullOrWhiteSpace(p)).Take(5).Select(p => Cut(p, 300)).ToList(),
            Recommendation = Cut(insight.Recommendation, 500)
        };
    }

    // The statistics as plain text for the AI
    private static string Describe(AnalyticsData d)
    {
        string Skills(int[]? s) => s == null ? "no data"
            : $"Teamwork {s[0]}, Problem Solving {s[1]}, Communication {s[2]}, Leadership {s[3]}";

        var text = new StringBuilder();
        text.AppendLine($"Scope: {d.DepartmentName ?? "whole company"}, {d.EmployeeCount} employees.");
        text.AppendLine($"Average soft-skills score: {d.AverageSkills?.ToString() ?? "no data"}/100.");
        text.AppendLine($"Company soft skills average: {Skills(d.CompanySkills)}.");
        if (d.DepartmentName != null)
        {
            text.AppendLine($"{d.DepartmentName} soft skills average: {Skills(d.DepartmentSkills)}.");
        }
        text.AppendLine($"Exam pass rate: {d.ExamPassRate?.ToString() ?? "no data"}% (pass mark 60).");
        foreach (var e in d.Exams)
        {
            text.AppendLine($"- Exam {e.Title}: {e.Attempts} attempts, pass rate {e.PassRate}%, average {e.AverageScore}.");
        }
        text.AppendLine($"Promotions in the last 6 months: {d.PromotionsLast6Months}. Ready for promotion now: {d.ReadyForPromotion}.");
        text.AppendLine($"Average promotion readiness: {d.AverageReadiness?.ToString() ?? "no data"}%.");
        text.AppendLine("Career status counts: " + string.Join(", ", d.Statuses.Select(s => $"{s.Status} {s.Count}")) + ".");
        foreach (var s in d.StuckCertificates)
        {
            text.AppendLine($"- Next certificate for {s.Employees} employees: {s.Title} ({s.Rejected} rejected uploads).");
        }
        return text.ToString();
    }

    private static string Cut(string? text, int max)
    {
        text = (text ?? string.Empty).Trim();
        return text.Length > max ? text[..max] : text;
    }
}
