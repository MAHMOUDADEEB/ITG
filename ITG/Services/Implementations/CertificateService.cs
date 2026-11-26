using ITG.Constants;
using ITG.Data;
using ITG.Models.Entities;
using ITG.Models.Enums;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

public class CertificateService : ICertificateService
{
    private readonly AppDbContext _context;
    private readonly ICareerService _careerService;
    private readonly IFileStorageService _storage;
    private readonly INotificationService _notifications;

    public CertificateService(AppDbContext context, ICareerService careerService, IFileStorageService storage,
        INotificationService notifications)
    {
        _context = context;
        _careerService = careerService;
        _storage = storage;
        _notifications = notifications;
    }

    public async Task<ServiceResult> UploadAsync(string employeeId, int certificateId, IFormFile? file, DateTime? issuedOn)
    {
        // ----- File checks -----
        if (file == null || file.Length == 0)
        {
            return ServiceResult.Fail("Please choose a file to upload.");
        }

        if (file.Length > AppConstants.MaxCertificateFileBytes)
        {
            return ServiceResult.Fail("The file is too large. The maximum size is 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AppConstants.AllowedCertificateExtensions.Contains(extension))
        {
            return ServiceResult.Fail("Only PDF, PNG or JPG files are allowed.");
        }

        // The extension can be faked, so we also check the real file signature
        if (!await HasValidSignatureAsync(file, extension))
        {
            return ServiceResult.Fail("The file content does not match its type.");
        }

        if (issuedOn.HasValue && issuedOn.Value.Date > DateTime.Today)
        {
            return ServiceResult.Fail("The issue date cannot be in the future.");
        }

        // ----- Roadmap checks -----
        var progress = await _careerService.GetProgressAsync(employeeId);
        var item = progress.Items.FirstOrDefault(i => i.CertificateId == certificateId);

        if (item == null)
        {
            return ServiceResult.Fail("This certificate is not part of your roadmap.");
        }

        if (item.Status is RoadmapItemStatus.Verified or RoadmapItemStatus.Pending)
        {
            return ServiceResult.Fail("This certificate has already been uploaded.");
        }

        // ----- Save -----
        var storedName = await _storage.SaveCertificateAsync(file, extension);

        _context.EmployeeCertificates.Add(new EmployeeCertificate
        {
            EmployeeId = employeeId,
            CertificateId = certificateId,
            FilePath = storedName,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = ContentTypeFor(extension),
            IssuedOn = issuedOn?.Date,
            Status = VerificationStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Tell HR there is something new to review
        var name = await _context.Employees.Where(e => e.Id == employeeId).Select(e => e.Name).FirstAsync();
        await _notifications.NotifyAdminsAsync("{0} uploaded \"{1}\" for review.", "bi-file-earmark-arrow-up",
            "/Admin/Reviews", name, item.Title);

        return ServiceResult.Ok();
    }

    public async Task<CertificateFile?> GetFileAsync(int submissionId, string userId, bool isAdmin)
    {
        var submission = await _context.EmployeeCertificates
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == submissionId);

        if (submission == null || (!isAdmin && submission.EmployeeId != userId))
        {
            return null;
        }

        var stream = _storage.OpenCertificate(submission.FilePath);
        return stream == null
            ? null
            : new CertificateFile(stream, submission.ContentType, submission.OriginalFileName);
    }

    // ---------------- Helpers ----------------

    private static string ContentTypeFor(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        _ => "image/jpeg"
    };

    private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 4)
        {
            return false;
        }

        return extension switch
        {
            ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46, // %PDF
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47, // .PNG
            _ => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF                             // JPEG
        };
    }
}
