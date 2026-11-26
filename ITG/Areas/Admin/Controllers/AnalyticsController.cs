using System.Globalization;
using ITG.Data;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using ITG.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ITG.Areas.Admin.Controllers;

// HR analytics: skills, exams, promotions and readiness, with an AI summary
public class AnalyticsController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly IAnalyticsService _analytics;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(AppDbContext context, IAnalyticsService analytics, ILogger<AnalyticsController> logger)
    {
        _context = context;
        _analytics = analytics;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? department) =>
        View(await BuildAsync(department, null, null));

    // Asks the AI to read the numbers and write a short analysis
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Insight(int? department)
    {
        var data = await _analytics.GetAsync(department);
        AnalyticsInsight? insight = null;
        string? error = null;

        try
        {
            insight = await _analytics.WriteInsightAsync(data, CultureInfo.CurrentUICulture.Name);
        }
        catch (AiUnavailableException ex)
        {
            _logger.LogWarning(ex, "Could not write the analytics summary");
            error = Localizer[ex.MessageKey].Value;
        }

        return View(nameof(Index), await BuildAsync(department, insight, error, data));
    }

    private async Task<AnalyticsViewModel> BuildAsync(int? department, AnalyticsInsight? insight, string? error,
        AnalyticsData? data = null)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == department))
            .ToListAsync();

        return new AnalyticsViewModel
        {
            Data = data ?? await _analytics.GetAsync(department),
            DepartmentId = department,
            Departments = departments,
            Insight = insight,
            InsightError = error
        };
    }
}
