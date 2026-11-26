using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface ICertificateReviewService
{
    Task<int> CountPendingAsync();
    Task<IReadOnlyList<PendingReview>> GetPendingAsync();
    Task<ServiceResult> ApproveAsync(int submissionId, string adminId);
    Task<ServiceResult> RejectAsync(int submissionId, string adminId, string? reason);
}
