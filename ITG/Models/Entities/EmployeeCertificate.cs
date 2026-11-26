using ITG.Models.Enums;

namespace ITG.Models.Entities;

// One uploaded certificate file, reviewed by an admin.
// If a submission is rejected, the employee uploads again (a new row), so history is kept.
public class EmployeeCertificate
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    public int CertificateId { get; set; }
    public Certificate Certificate { get; set; } = null!;

    // Stored file name inside the private certificates folder (not under wwwroot)
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    // Date written on the certificate itself
    public DateTime? IssuedOn { get; set; }

    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    // Admin who approved or rejected
    public string? ReviewedById { get; set; }
    public Employee? ReviewedBy { get; set; }
}
