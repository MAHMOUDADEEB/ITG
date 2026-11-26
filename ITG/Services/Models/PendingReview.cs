namespace ITG.Services.Models;

// One uploaded certificate waiting for HR review
public record PendingReview(
    int SubmissionId,
    string EmployeeId,
    string EmployeeName,
    string EmployeeNumber,
    string? DepartmentName,
    string CertificateTitle,
    string OriginalFileName,
    DateTime SubmittedAt,
    DateTime? IssuedOn);
