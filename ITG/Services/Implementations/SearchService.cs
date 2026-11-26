using ITG.Data;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

// Site search for employees: certificates and exams.
// (Pages are matched in the controller, because their names are translated.)
public class SearchService : ISearchService
{
    private const int MaxResults = 20;

    private readonly AppDbContext _context;
    private readonly ICareerService _careerService;
    private readonly IExamService _examService;

    public SearchService(AppDbContext context, ICareerService careerService, IExamService examService)
    {
        _context = context;
        _careerService = careerService;
        _examService = examService;
    }

    public async Task<SearchResults> SearchAsync(string employeeId, string query)
    {
        // ----- Certificates (SQL Server compares text without case) -----
        var certificates = await _context.Certificates
            .AsNoTracking()
            .Where(c => c.IsActive &&
                        (c.Title.Contains(query) || c.Category.Contains(query) ||
                         (c.Description != null && c.Description.Contains(query))))
            .OrderBy(c => c.Title)
            .Take(MaxResults)
            .Select(c => new { c.Id, c.Title, c.Category, c.Description })
            .ToListAsync();

        // Status of each certificate in the employee's roadmap
        var progress = await _careerService.GetProgressAsync(employeeId);
        var roadmap = progress.Items.ToDictionary(i => i.CertificateId, i => i.Status);

        var certificateHits = certificates
            .Select(c => new CertificateHit(c.Id, c.Title, c.Category, c.Description,
                roadmap.TryGetValue(c.Id, out var status) ? status : null))
            // Certificates in the employee's roadmap first
            .OrderBy(c => c.RoadmapStatus == null)
            .ThenBy(c => c.Title)
            .ToList();

        // ----- Exams: reuse the Exams page cards, so state and score are the same -----
        var overview = await _examService.GetOverviewAsync(employeeId);
        var examHits = overview.Exams
            .Where(e => Matches(e.Title, query) || Matches(e.Category, query) || Matches(e.Description, query))
            .Take(MaxResults)
            .ToList();

        return new SearchResults(certificateHits, examHits);
    }

    private static bool Matches(string? text, string query) =>
        text != null && text.Contains(query, StringComparison.CurrentCultureIgnoreCase);
}
