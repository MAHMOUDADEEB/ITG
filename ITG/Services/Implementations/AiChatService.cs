using ITG.Data;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

// The "AI" page: a simple text chat. The assistant knows the employee's
// role, target role and roadmap, so it can give personal career advice.
public class AiChatService : IAiChatService
{
    private const int MaxMessages = 12;
    private const int MaxMessageLength = 2000;

    private readonly AppDbContext _context;
    private readonly IAiClient _ai;
    private readonly ICareerService _careerService;

    public AiChatService(AppDbContext context, IAiClient ai, ICareerService careerService)
    {
        _context = context;
        _ai = ai;
        _careerService = careerService;
    }

    public async Task<string> ReplyAsync(string employeeId, IReadOnlyList<AiMessage> conversation, string cultureName)
    {
        // Keep only the latest messages and limit their size (cost + safety)
        var messages = conversation
            .Where(m => !string.IsNullOrWhiteSpace(m.Text))
            .TakeLast(MaxMessages)
            .Select(m => m with { Text = m.Text.Length > MaxMessageLength ? m.Text[..MaxMessageLength] : m.Text })
            .ToList();

        // The API expects the conversation to start with the user
        while (messages.Count > 0 && !messages[0].FromUser)
        {
            messages.RemoveAt(0);
        }

        if (messages.Count == 0)
        {
            return string.Empty;
        }

        return await _ai.GenerateAsync(await BuildContextAsync(employeeId, cultureName), messages);
    }

    private async Task<string> BuildContextAsync(string employeeId, string cultureName)
    {
        var name = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => e.Name)
            .FirstOrDefaultAsync() ?? "Employee";

        var progress = await _careerService.GetProgressAsync(employeeId);
        var roadmap = progress.Items.Count == 0
            ? "none"
            : string.Join("; ", progress.Items.Select(i => $"{i.Title} ({StatusText(i.Status)})"));

        var target = progress.IsAtFork && !progress.HasTarget
            ? "not chosen yet (options: " + string.Join(", ", progress.PathOptions.Select(o => o.PositionTitle)) + ")"
            : progress.TargetPositionTitle ?? "none";

        return $"""
            You are the ITG Learning Assistant, a friendly career and learning coach inside ITG's employee platform.
            You are talking with {name}.
            Current role: {progress.CurrentPositionTitle ?? "not assigned"}. Target role: {target}.
            Roadmap certificates: {roadmap}. Progress: {progress.Percent}%.
            How the platform works: employees earn certificates outside the platform, upload them (PDF or image)
            in the Roadmap page, and HR approves them. When all certificates are approved, HR can promote the employee.
            The Exams page has AI practice exams for self-evaluation only.
            Rules: answer general questions helpfully and clearly; keep answers short (under 150 words) unless asked for more;
            use simple formatting (short paragraphs or "-" bullet lines); never invent company policies, salaries or promotion dates;
            reply in the same language the user writes in (default: {AiJson.LanguageName(cultureName)}).
            """;
    }

    private static string StatusText(RoadmapItemStatus status) => status switch
    {
        RoadmapItemStatus.Verified => "completed",
        RoadmapItemStatus.Pending => "waiting for HR review",
        RoadmapItemStatus.Rejected => "rejected, needs a new upload",
        _ => "not uploaded"
    };
}
