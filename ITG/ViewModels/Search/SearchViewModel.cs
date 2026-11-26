using ITG.Services.Models;

namespace ITG.ViewModels.Search;

// A page of the site that matches the search
public record PageHit(string Controller, string Icon, string Title, string Description);

public class SearchViewModel
{
    public string Query { get; init; } = string.Empty;

    // False when the text is empty or too short to search
    public bool HasQuery { get; init; }

    public IReadOnlyList<PageHit> Pages { get; init; } = Array.Empty<PageHit>();
    public IReadOnlyList<CertificateHit> Certificates { get; init; } = Array.Empty<CertificateHit>();
    public IReadOnlyList<ExamCard> Exams { get; init; } = Array.Empty<ExamCard>();

    public int TotalCount => Pages.Count + Certificates.Count + Exams.Count;
}
