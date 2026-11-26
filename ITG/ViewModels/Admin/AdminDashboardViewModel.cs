using ITG.Services.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ITG.ViewModels.Admin;

public class AdminDashboardViewModel
{
    // ----- Summary cards -----
    public int TotalEmployees { get; init; }
    public int ReadyForPromotion { get; init; }
    public int PendingReviews { get; init; }

    // ----- Filters (kept in the URL so pages can be shared and refreshed) -----
    public string? Search { get; init; }
    public int? DepartmentId { get; init; }
    public CareerStatus? Status { get; init; }
    public IReadOnlyList<SelectListItem> Departments { get; init; } = Array.Empty<SelectListItem>();

    // ----- Table + paging -----
    public IReadOnlyList<EmployeeRow> Rows { get; init; } = Array.Empty<EmployeeRow>();
    public int MatchingCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 6;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(MatchingCount / (double)PageSize));
    public int FirstItem => MatchingCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItem => Math.Min(Page * PageSize, MatchingCount);

    public static readonly int[] PageSizeOptions = { 6, 10, 20 };
}

// One employee line in the admin table
public record EmployeeRow(
    string Id,
    string Name,
    string EmployeeNumber,
    string? DepartmentName,
    string? CurrentRole,
    int Percent,
    CareerStatus Status,
    int PendingCount)
{
    // "Omar Haddad" -> "OH"
    public string Initials => string.Concat(Name
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));

    // Stable avatar color (1-6) based on the employee number
    public int ColorIndex => EmployeeNumber.Sum(c => (int)c) % 6 + 1;
}
