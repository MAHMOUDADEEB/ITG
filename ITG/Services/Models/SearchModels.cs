namespace ITG.Services.Models;

// A certificate found by the search, with its status in the employee's roadmap (null = not in the roadmap)
public record CertificateHit(int CertificateId, string Title, string Category, string? Description, RoadmapItemStatus? RoadmapStatus);

// An exam found by the search (the card has the state, score and links)
public record SearchResults(IReadOnlyList<CertificateHit> Certificates, IReadOnlyList<ExamCard> Exams);
