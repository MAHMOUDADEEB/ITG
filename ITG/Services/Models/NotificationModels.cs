namespace ITG.Services.Models;

public record NotificationItem(int Id, string MessageKey, string[] Arguments, string Icon, string? Url,
    bool IsRead, DateTime CreatedAt);

public record NotificationSummary(int UnreadCount, IReadOnlyList<NotificationItem> Items)
{
    public static NotificationSummary Empty { get; } = new(0, Array.Empty<NotificationItem>());
}
