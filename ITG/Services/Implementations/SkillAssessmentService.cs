using ITG.Data;
using ITG.Models.Entities;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ITG.Services.Implementations;

// AI soft-skills assessment.
// 1) The AI writes scenario questions; each option has hidden points (0-3).
// 2) The server scores the answers (fair and repeatable, not left to the AI).
// 3) The AI writes a short feedback summary from the scores.
public class SkillAssessmentService : ISkillAssessmentService
{
    private const int QuestionsPerSkill = 4;
    private static readonly TimeSpan DraftLifetime = TimeSpan.FromHours(2);

    private readonly AppDbContext _context;
    private readonly IAiClient _ai;
    private readonly IMemoryCache _cache;
    private readonly ICareerService _careerService;
    private readonly ILogger<SkillAssessmentService> _logger;

    public SkillAssessmentService(AppDbContext context, IAiClient ai, IMemoryCache cache,
        ICareerService careerService, ILogger<SkillAssessmentService> logger)
    {
        _context = context;
        _ai = ai;
        _cache = cache;
        _careerService = careerService;
        _logger = logger;
    }

    public Task<SkillAssessment?> GetLatestAsync(string employeeId) =>
        _context.SkillAssessments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

    // The assessment before the latest one (null if there is only one), for comparison
    public Task<SkillAssessment?> GetPreviousAsync(string employeeId) =>
        _context.SkillAssessments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(1)
            .FirstOrDefaultAsync();

    public async Task<bool> CanTakeAsync(string employeeId)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.CanRetakeSkillsAssessment, HasTaken = e.SkillAssessments.Any() })
            .FirstOrDefaultAsync();

        return employee != null && (!employee.HasTaken || employee.CanRetakeSkillsAssessment);
    }

    public async Task<IReadOnlyList<AssessmentQuestionView>> StartAsync(string employeeId, string cultureName)
    {
        // Reopening the page shows the same questions instead of calling the AI again
        if (!_cache.TryGetValue(DraftKey(employeeId), out AssessmentDraft? draft) || draft == null)
        {
            var progress = await _careerService.GetProgressAsync(employeeId);
            draft = new AssessmentDraft(await GenerateQuestionsAsync(progress.CurrentPositionTitle, cultureName), DateTime.UtcNow);
            _cache.Set(DraftKey(employeeId), draft, DraftLifetime);
        }

        return draft.Questions
            .Select(q => new AssessmentQuestionView(q.Skill, q.Scenario, q.Options.Select(o => o.Text).ToList()))
            .ToList();
    }

    public async Task<ServiceResult> SubmitAsync(string employeeId, IReadOnlyList<int?> answers, string cultureName)
    {
        if (!await CanTakeAsync(employeeId))
        {
            return ServiceResult.Fail("You have already completed the skills assessment.");
        }

        if (!_cache.TryGetValue(DraftKey(employeeId), out AssessmentDraft? draft) || draft == null)
        {
            return ServiceResult.Fail("Your assessment session expired. Please start again.");
        }

        if (answers.Count < draft.Questions.Count || answers.Take(draft.Questions.Count).Any(a => a == null))
        {
            return ServiceResult.Fail("Please answer all the questions.");
        }

        // ----- Score each skill: points earned / maximum points -----
        var details = draft.Questions
            .Select((q, i) =>
            {
                var index = Math.Clamp(answers[i]!.Value, 0, q.Options.Count - 1);
                return new AssessmentAnswerDetail(q.Skill, q.Scenario, q.Options[index].Text, q.Options[index].Points);
            })
            .ToList();

        int ScoreFor(string skill)
        {
            var forSkill = details.Where(d => d.Skill == skill).ToList();
            return forSkill.Count == 0 ? 0 : (int)Math.Round(forSkill.Sum(d => d.Points) * 100.0 / (forSkill.Count * 3));
        }

        var assessment = new SkillAssessment
        {
            EmployeeId = employeeId,
            TeamworkScore = ScoreFor(SoftSkills.Teamwork),
            ProblemSolvingScore = ScoreFor(SoftSkills.ProblemSolving),
            CommunicationScore = ScoreFor(SoftSkills.Communication),
            LeadershipScore = ScoreFor(SoftSkills.Leadership),
            DetailsJson = AiJson.Serialize(details),
            CreatedAt = DateTime.UtcNow
        };
        assessment.OverallScore = (int)Math.Round((assessment.TeamworkScore + assessment.ProblemSolvingScore
            + assessment.CommunicationScore + assessment.LeadershipScore) / 4.0);
        var summary = await WriteSummaryAsync(assessment, cultureName);
        assessment.Summary = string.IsNullOrWhiteSpace(summary) ? null : summary;

        _context.SkillAssessments.Add(assessment);

        // Lock the assessment again (only an admin can re-open it)
        var employee = await _context.Employees.FirstAsync(e => e.Id == employeeId);
        employee.CanRetakeSkillsAssessment = false;

        await _context.SaveChangesAsync();
        _cache.Remove(DraftKey(employeeId));

        return ServiceResult.Ok();
    }

    // ---------------- Helpers ----------------

    private static string DraftKey(string employeeId) => $"skills-assessment:{employeeId}";

    private async Task<List<AssessmentQuestion>> GenerateQuestionsAsync(string? position, string cultureName)
    {
        var language = AiJson.LanguageName(cultureName);
        var total = QuestionsPerSkill * SoftSkills.All.Length;

        var system = $$"""
            You design soft-skills assessments for ITG, a technology company.
            Create exactly {{total}} workplace scenario questions for an employee working as "{{position ?? "employee"}}":
            {{QuestionsPerSkill}} for each skill: Teamwork, Problem Solving, Communication, Leadership.
            Each question is a short realistic situation followed by "What would you do?".
            Each question has exactly 4 options. Give points to the options: 3 = best behaviour, 2 = good, 1 = weak, 0 = poor.
            Use each of the values 3, 2, 1 and 0 exactly once per question. Do not make the best answer obvious.
            Write the scenarios and options in {{language}}. Keep the "skill" value in English exactly as listed.
            Answer with JSON only, in this exact shape:
            {"questions":[{"skill":"Teamwork","scenario":"...","options":[{"text":"...","points":3},{"text":"...","points":0},{"text":"...","points":2},{"text":"...","points":1}]}]}
            """;

        var text = await _ai.GenerateAsync(system,
            new[] { new AiMessage(true, "Create the assessment now.") }, json: true);

        var generated = AiJson.Parse<GeneratedAssessment>(text);

        var valid = (generated.Questions ?? new List<AssessmentQuestion>())
            .Where(q => SoftSkills.All.Contains(q.Skill)
                        && !string.IsNullOrWhiteSpace(q.Scenario)
                        && q.Options is { Count: 4 }
                        && q.Options.All(o => !string.IsNullOrWhiteSpace(o.Text) && o.Points is >= 0 and <= 3))
            .Select(q => q with
            {
                Scenario = q.Scenario.Trim(),
                Options = q.Options.OrderBy(_ => Random.Shared.Next()).ToList()
            })
            .ToList();

        // Every skill must be measured by at least 2 questions
        if (SoftSkills.All.Any(skill => valid.Count(q => q.Skill == skill) < 2))
        {
            throw new AiUnavailableException(AiFailure.BadResponse, text);
        }

        // Questions are mixed so the same skill does not come four times in a row
        return valid.OrderBy(_ => Random.Shared.Next()).ToList();
    }

    private async Task<string> WriteSummaryAsync(SkillAssessment result, string cultureName)
    {
        var language = AiJson.LanguageName(cultureName);
        var system = $"""
            You are a friendly career coach at ITG. Write 2 short sentences of feedback in {language}
            for an employee's soft-skills result (scores out of 100). Mention the strongest skill
            and one practical tip for the weakest skill. Plain text only, no lists.
            """;
        var scores = $"Teamwork {result.TeamworkScore}, Problem Solving {result.ProblemSolvingScore}, " +
                     $"Communication {result.CommunicationScore}, Leadership {result.LeadershipScore}.";

        try
        {
            var summary = await _ai.GenerateAsync(system, new[] { new AiMessage(true, scores) });
            return summary.Length > 2000 ? summary[..2000] : summary;
        }
        catch (AiUnavailableException ex)
        {
            // The result is still saved; only the feedback text is missing
            _logger.LogWarning(ex, "Could not write the assessment summary");
            return string.Empty;
        }
    }

    private sealed record GeneratedAssessment(List<AssessmentQuestion>? Questions);
}
