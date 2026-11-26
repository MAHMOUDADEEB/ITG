using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface ICertificateService
{
    // Validates and stores an uploaded certificate; it waits for admin review
    Task<ServiceResult> UploadAsync(string employeeId, int certificateId, IFormFile? file, DateTime? issuedOn);

    // Opens an uploaded file. Employees can open only their own files; admins can open all.
    Task<CertificateFile?> GetFileAsync(int submissionId, string userId, bool isAdmin);
}
