namespace ITG.Models.Entities;

// Join table: the ordered list of certificates (roadmap) required to reach a position
public class PositionCertificate
{
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public int CertificateId { get; set; }
    public Certificate Certificate { get; set; } = null!;

    // Display order inside the roadmap (1, 2, 3, ...)
    public int SortOrder { get; set; }
}
