using System.Text;
using ITG.Data;
using ITG.Models.Entities;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

// "My Plan": a personal 4-week plan written by the AI from everything the platform knows
// about the employee (roadmap, exams, soft skills).
public class DevelopmentPlanService : IDevelopmentPlanService
{
    private const int Weeks = 4;
    private const int MaxTasksPerWeek = 4;
    private const int MaxFocus = 3;
    private static readonly string[] TaskTypes = { "certificate", "exam", "skill", "practice" };

    private readonly AppDbContext _context;
    private readonly IAiClient _ai;
    private readonly ICareerService _careerService;
    private readonly IExamService _examService;
    private readonly ISkillAssessmentService _assessmentService;
    private readonly ILogger<DevelopmentPlanService> _logger;

    public DevelopmentPlanService(AppDbContext context, IAiClient ai, ICareerService careerService,
        IExamService examService, ISkillAssessmentService assessmentService, ILogger<DevelopmentPlanService> logger)
    {
        _context = context;
        _ai = ai;
        _careerService = careerService;
        _examService = examService;
        _assessmentService = assessmentService;
        _logger = logger;
    }

    public async Task<PlanView?> GetCurrentAsync(string employeeId)
    {
        var plan = await _context.DevelopmentPlans
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var content = plan == null ? null : ReadContent(plan.ContentJson);
        return content == null ? null : new PlanView(plan!.Id, plan.CreatedAt, plan.Language, content);
    }

    public async Task<ServiceResult> GenerateAsync(string employeeId, string cultureName)
    {
        PlanContent content;
        try
        {
            var system = BuildInstructions(AiJson.LanguageName(cultureName));
            var profile = await BuildProfileAsync(employeeId);
            var answer = await _ai.GenerateAsync(system, new[] { new AiMessage(true, profile) }, json: true);
            content = Clean(AiJson.Parse<PlanContent>(answer));
        }
        catch (AiUnavailableException ex)
        {
            _logger.LogWarning(ex, "Could not create the development plan");
            return ServiceResult.Fail(ex.MessageKey);
        }

        if (content.Weeks.Count == 0)
        {
            return ServiceResult.Fail("The AI service is not available right now. Please try again in a moment.");
        }

        // Only the latest plan is kept
        await _context.DevelopmentPlans.Where(p => p.EmployeeId == employeeId).ExecuteDeleteAsync();

        _context.DevelopmentPlans.Add(new DevelopmentPlan
        {
            EmployeeId = employeeId,
            ContentJson = AiJson.Serialize(content),
            Language = cultureName.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<PlanTaskResult?> SetTaskDoneAsync(string employeeId, int planId, int week, int task, bool done)
    {
        var plan = await _context.DevelopmentPlans.FirstOrDefaultAsync(p => p.Id == planId && p.EmployeeId == employeeId);
        var content = plan == null ? null : ReadContent(plan.ContentJson);
        if (content == null || week < 0 || week >= content.Weeks.Count
            || task < 0 || task >= content.Weeks[week].Tasks.Count)
        {
            return null;
        }

        content.Weeks[week].Tasks[task].Done = done;
        plan!.ContentJson = AiJson.Serialize(content);
        await _context.SaveChangesAsync();

        var view = new PlanView(plan.Id, plan.CreatedAt, plan.Language, content);
        return new PlanTaskResult(view.DoneTasks, view.TotalTasks, view.Percent);
    }

    // ---------------- Prompt ----------------

    private static string BuildInstructions(string language) => $$"""
        You are a senior career coach at ITG (a technology company). Write a personal development plan
        for one employee, in {{language}}, based on the profile you receive.
        The plan has exactly {{Weeks}} weeks. Each week has a short title and 3 tasks
        (at most {{MaxTasksPerWeek}}). Tasks must be concrete, realistic for a busy employee
        (1-3 hours each), and use the real names of the certificates and exams in the profile.
        Order the work smartly: start with the next certificate and the weakest areas, use the AI practice
        exams to check knowledge, and include soft-skill practice for the weakest skill.
        Also give a 2-sentence summary and 2-3 focus areas with a one-sentence reason each.
        Never invent company policies, salaries or promotion dates.
        Answer with JSON only, in this exact shape:
        {"summary":"...","focus":[{"title":"...","reason":"..."}],
         "weeks":[{"title":"...","tasks":[{"text":"...","type":"certificate|exam|skill|practice"}]}]}
        """;

    private async Task<string> BuildProfileAsync(string employeeId)
    {
        var progress = await _careerService.GetProgressAsync(employeeId);
        var exams = (await _examService.GetOverviewAsync(employeeId)).Exams;
        var skills = await _assessmentService.GetLatestAsync(employeeId);

        var text = new StringBuilder();
        text.AppendLine($"Current role: {progress.CurrentPositionTitle ?? "not assigned"}.");
        text.AppendLine($"Next role: {progress.TargetPositionTitle ?? "not set yet (focus on excelling in the current role)"}.");

        text.AppendLine("Required certificates for the next role:");
        if (progress.Items.Count == 0)
        {
            text.AppendLine("- none");
        }
        foreach (var item in progress.Items)
        {
            var status = item.Status switch
            {
                RoadmapItemStatus.Verified => "done",
                RoadmapItemStatus.Pending => "uploaded, waiting for HR review",
                RoadmapItemStatus.Rejected => "rejected by HR, must be redone",
                _ => "not started"
            };
            text.AppendLine($"- {item.Title} ({item.Category}): {status}");
        }

        text.AppendLine("AI practice exams (pass mark 60/100):");
        if (exams.Count == 0)
        {
            text.AppendLine("- none");
        }
        foreach (var exam in exams)
        {
            var role = exam.ForNextRole ? "next role" : "current role";
            var result = exam.LatestScore.HasValue ? $"latest score {exam.LatestScore}/100" : "not taken yet";
            text.AppendLine($"- {exam.Title} ({role}): {result}");
        }

        text.AppendLine(skills == null
            ? "Soft skills: not assessed yet (suggest taking the Skills Assessment)."
            : $"Soft skills (out of 100): Teamwork {skills.TeamworkScore}, Problem Solving {skills.ProblemSolvingScore}, " +
              $"Communication {skills.CommunicationScore}, Leadership {skills.LeadershipScore}.");

        return text.ToString();
    }

    // ---------------- Helpers ----------------

    // Keeps the AI answer within safe sizes
    private static PlanContent Clean(PlanContent raw) => new()
    {
        Summary = Cut(raw.Summary, 600),
        Focus = (raw.Focus ?? new())
            .Where(f => !string.IsNullOrWhiteSpace(f.Title))
            .Take(MaxFocus)
            .Select(f => new PlanFocus { Title = Cut(f.Title, 80), Reason = Cut(f.Reason, 250) })
            .ToList(),
        Weeks = (raw.Weeks ?? new())
            .Take(Weeks)
            .Select(w => new PlanWeek
            {
                Title = Cut(w.Title, 100),
                Tasks = (w.Tasks ?? new())
                    .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                    .Take(MaxTasksPerWeek)
                    .Select(t => new PlanTask
                    {
                        Text = Cut(t.Text, 250),
                        Type = TaskTypes.Contains(t.Type?.Trim().ToLowerInvariant()) ? t.Type!.Trim().ToLowerInvariant() : "practice"
                    })
                    .ToList()
            })
            .Where(w => w.Tasks.Count > 0)
            .ToList()
    };

    private static string Cut(string? text, int max)
    {
        text = (text ?? string.Empty).Trim();
        return text.Length > max ? text[..max] : text;
    }

    private static PlanContent? ReadContent(string json)
    {
        try
        {
            return AiJson.Deserialize<PlanContent>(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
