using ITG.Services.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ITG.ViewModels.Admin;

public class AnalyticsViewModel
{
    public AnalyticsData Data { get; init; } = new();
    public int? DepartmentId { get; init; }
    public IReadOnlyList<SelectListItem> Departments { get; init; } = Array.Empty<SelectListItem>();

    // AI summary (only after the admin asks for it)
    public AnalyticsInsight? Insight { get; init; }
    public string? InsightError { get; init; }
}
