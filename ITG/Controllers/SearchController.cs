using ITG.Services.Interfaces;
using ITG.ViewModels.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ITG.Controllers;

// Search box in the top bar: pages, certificates and exams
public class SearchController : EmployeeBaseController
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    // Pages that can be found, with extra words people may type to find them
    private static readonly (string Controller, string Icon, string Title, string Description, string[] Keywords)[] SitePages =
    {
        ("Home", "bi-house-door-fill", "Home", "Your career goal, AI evaluation and roadmap.",
            new[] { "goal", "career", "promotion", "next role" }),
        ("Dashboard", "bi-grid-1x2", "Dashboard", "Career progress, last exam and next step.",
            new[] { "progress", "overview", "next step", "last exam" }),
        ("Exams", "bi-card-checklist", "Exams", "AI practice exams to test your knowledge.",
            new[] { "exam", "test", "quiz", "practice", "score" }),
        ("SkillsAssessment", "bi-clipboard-check", "Assess Your Skills", "AI soft-skills assessment (taken once).",
            new[] { "assessment", "soft skills", "teamwork", "leadership", "communication", "problem solving" }),
        ("AiAssistant", "bi-stars", "AI", "Ask the AI assistant about your skills and career.",
            new[] { "assistant", "chat", "help", "question" }),
        ("Plan", "bi-journal-check", "My Plan", "Your personal 4-week development plan written by AI.",
            new[] { "plan", "development", "goals", "weekly", "tasks", "coach" }),
        ("Roadmap", "bi-signpost-split", "Roadmap", "Required certificates for your next role. Upload them here.",
            new[] { "certificate", "upload", "path", "requirements", "fork" }),
        ("Records", "bi-clipboard-data", "Records", "Passed exams, verified certificates and skills score.",
            new[] { "record", "history", "results", "passed", "earned" })
    };

    private readonly ISearchService _searchService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SearchController(ISearchService searchService, IStringLocalizer<SharedResource> localizer)
    {
        _searchService = searchService;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = (q ?? string.Empty).Trim();
        if (query.Length > MaxLength)
        {
            query = query[..MaxLength];
        }

        if (query.Length < MinLength)
        {
            return View(new SearchViewModel { Query = query, HasQuery = false });
        }

        var results = await _searchService.SearchAsync(CurrentUserId, query);

        return View(new SearchViewModel
        {
            Query = query,
            HasQuery = true,
            Pages = FindPages(query),
            Certificates = results.Certificates,
            Exams = results.Exams
        });
    }

    // Matches the English name, the translated name, the description and the extra words
    private List<PageHit> FindPages(string query)
    {
        bool Has(string text) => text.Contains(query, StringComparison.CurrentCultureIgnoreCase);

        return SitePages
            .Select(p => new
            {
                Page = p,
                Title = _localizer[p.Title].Value,
                Description = _localizer[p.Description].Value
            })
            .Where(x => Has(x.Page.Title) || Has(x.Title) || Has(x.Page.Description) || Has(x.Description)
                        || x.Page.Keywords.Any(k => Has(k) || query.Contains(k, StringComparison.CurrentCultureIgnoreCase)))
            .Select(x => new PageHit(x.Page.Controller, x.Page.Icon, x.Title, x.Description))
            .ToList();
    }
}
