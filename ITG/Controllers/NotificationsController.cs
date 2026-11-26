using System.Security.Claims;
using ITG.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITG.Controllers;

// Bell menu in the top bar (employees and admins)
[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Marks the notification as read and opens its page
    public async Task<IActionResult> Open(int id)
    {
        var url = await _notifications.OpenAsync(CurrentUserId, id);
        return !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url)
            ? LocalRedirect(url)
            : RedirectToAction("Index", "Home", new { area = "" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll(string? returnUrl)
    {
        await _notifications.MarkAllReadAsync(CurrentUserId);
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home", new { area = "" });
    }
}
