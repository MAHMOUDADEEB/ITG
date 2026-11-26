using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface INotificationService
{
    // messageKey is a translation key; args fill its {0}, {1} ...
    Task NotifyAsync(string employeeId, string messageKey, string icon, string? url, params string[] args);

    // Sends the same notification to every admin
    Task NotifyAdminsAsync(string messageKey, string icon, string? url, params string[] args);

    Task<NotificationSummary> GetSummaryAsync(string userId, int take = 8);

    // Marks one notification as read and returns the page to open
    Task<string?> OpenAsync(string userId, int notificationId);

    Task MarkAllReadAsync(string userId);
}
