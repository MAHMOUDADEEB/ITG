using ITG.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ITG.Areas.Admin.Controllers;

// Certificates uploaded by employees and waiting for HR approval
public class ReviewsController : AdminBaseController
{
    private readonly ICertificateReviewService _reviewService;

    public ReviewsController(ICertificateReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _reviewService.GetPendingAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? returnUrl)
    {
        var result = await _reviewService.ApproveAsync(id, CurrentUserId);

        if (result.Succeeded)
        {
            ShowSuccess("The certificate was approved.");
        }
        else
        {
            ShowError(result.ErrorKey!);
        }

        return RedirectToLocal(returnUrl, nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason, string? returnUrl)
    {
        var result = await _reviewService.RejectAsync(id, CurrentUserId, reason);

        if (result.Succeeded)
        {
            ShowSuccess("The certificate was rejected and the employee can upload it again.");
        }
        else
        {
            ShowError(result.ErrorKey!);
        }

        return RedirectToLocal(returnUrl, nameof(Index));
    }
}
