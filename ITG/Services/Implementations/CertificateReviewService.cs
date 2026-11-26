using ITG.Data;
using ITG.Models.Enums;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

public class CertificateReviewService : ICertificateReviewService
{
    private const int MaxReasonLength = 500;
    private readonly AppDbContext _context;
    private readonly ICareerService _careerService;
    private readonly INotificationService _notifications;

    public CertificateReviewService(AppDbContext context, ICareerService careerService, INotificationService notifications)
    {
        _context = context;
        _careerService = careerService;
        _notifications = notifications;
    }

    public Task<int> CountPendingAsync() =>
        _context.EmployeeCertificates.CountAsync(s => s.Status == VerificationStatus.Pending);

    public async Task<IReadOnlyList<PendingReview>> GetPendingAsync() =>
        await _context.EmployeeCertificates
            .AsNoTracking()
            .Where(s => s.Status == VerificationStatus.Pending)
            .OrderBy(s => s.SubmittedAt)
            .Select(s => new PendingReview(
                s.Id,
                s.EmployeeId,
                s.Employee.Name,
                s.Employee.EmployeeNumber,
                s.Employee.Department != null ? s.Employee.Department.Name : null,
                s.Certificate.Title,
                s.OriginalFileName,
                s.SubmittedAt,
                s.IssuedOn))
            .ToListAsync();

    public async Task<ServiceResult> ApproveAsync(int submissionId, string adminId)
    {
        var submission = await _context.EmployeeCertificates.FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission == null || submission.Status != VerificationStatus.Pending)
        {
            return ServiceResult.Fail("This certificate is no longer waiting for review.");
        }

        submission.Status = VerificationStatus.Verified;
        submission.RejectionReason = null;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedById = adminId;

        await _context.SaveChangesAsync();

        var info = await InfoAsync(submission.EmployeeId, submission.CertificateId);
        await _notifications.NotifyAsync(submission.EmployeeId, "Your certificate \"{0}\" was approved.",
            "bi-patch-check-fill", "/Roadmap", info.Certificate);

        // That was the last certificate: the employee can be promoted now
        var progress = await _careerService.GetProgressAsync(submission.EmployeeId);
        if (progress.IsReadyForPromotion && progress.TargetPositionTitle != null)
        {
            await _notifications.NotifyAdminsAsync("{0} completed all requirements for {1}.", "bi-award",
                $"/Admin/Employees/Details/{submission.EmployeeId}", info.Employee, progress.TargetPositionTitle);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RejectAsync(int submissionId, string adminId, string? reason)
    {
        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason))
        {
            return ServiceResult.Fail("Please write the reason for rejection.");
        }

        if (reason.Length > MaxReasonLength)
        {
            reason = reason[..MaxReasonLength];
        }

        var submission = await _context.EmployeeCertificates.FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission == null || submission.Status != VerificationStatus.Pending)
        {
            return ServiceResult.Fail("This certificate is no longer waiting for review.");
        }

        submission.Status = VerificationStatus.Rejected;
        submission.RejectionReason = reason;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedById = adminId;

        await _context.SaveChangesAsync();

        var info = await InfoAsync(submission.EmployeeId, submission.CertificateId);
        await _notifications.NotifyAsync(submission.EmployeeId,
            "Your certificate \"{0}\" was rejected. See the reason and upload it again.",
            "bi-x-octagon", $"/Roadmap#cert-{submission.CertificateId}", info.Certificate);

        return ServiceResult.Ok();
    }

    // Names used in the notification texts
    private async Task<(string Employee, string Certificate)> InfoAsync(string employeeId, int certificateId)
    {
        var employee = await _context.Employees.Where(e => e.Id == employeeId).Select(e => e.Name).FirstAsync();
        var certificate = await _context.Certificates.Where(c => c.Id == certificateId).Select(c => c.Title).FirstAsync();
        return (employee, certificate);
    }
}
