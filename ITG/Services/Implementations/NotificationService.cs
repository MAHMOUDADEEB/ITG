using ITG.Constants;
using ITG.Data;
using ITG.Models.Entities;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

public class NotificationService : INotificationService
{
    // Old notifications are not needed in the menu
    private const int KeepPerUser = 50;

    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task NotifyAsync(string employeeId, string messageKey, string icon, string? url, params string[] args) =>
        AddAsync(new[] { employeeId }, messageKey, icon, url, args);

    public async Task NotifyAdminsAsync(string messageKey, string icon, string? url, params string[] args)
    {
        var adminIds = await _context.UserRoles
            .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Admin))
            .Select(ur => ur.UserId)
            .ToListAsync();

        await AddAsync(adminIds, messageKey, icon, url, args);
    }

    public async Task<NotificationSummary> GetSummaryAsync(string userId, int take = 8)
    {
        var unread = await _context.Notifications.CountAsync(n => n.EmployeeId == userId && !n.IsRead);

        var rows = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.EmployeeId == userId)
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Take(take)
            .Select(n => new { n.Id, n.MessageKey, n.Arguments, n.Icon, n.Url, n.IsRead, n.CreatedAt })
            .ToListAsync();

        var items = rows
            .Select(n => new NotificationItem(n.Id, n.MessageKey, AiJson.Deserialize<string[]>(n.Arguments) ?? Array.Empty<string>(),
                n.Icon, n.Url, n.IsRead, n.CreatedAt))
            .ToList();

        return new NotificationSummary(unread, items);
    }

    public async Task<string?> OpenAsync(string userId, int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.EmployeeId == userId);
        if (notification == null)
        {
            return null;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return notification.Url;
    }

    public Task MarkAllReadAsync(string userId) =>
        _context.Notifications
            .Where(n => n.EmployeeId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    private async Task AddAsync(IEnumerable<string> userIds, string messageKey, string icon, string? url, string[] args)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var arguments = args.Length == 0 ? null : AiJson.Serialize(args);
        foreach (var id in ids)
        {
            _context.Notifications.Add(new Notification
            {
                EmployeeId = id,
                MessageKey = messageKey,
                Arguments = arguments,
                Icon = icon,
                Url = url,
                CreatedAt = DateTime.UtcNow
            });
        }

        try
        {
            await _context.SaveChangesAsync();

            // Keep only the latest notifications of each user
            foreach (var id in ids)
            {
                var cutoff = await _context.Notifications
                    .Where(n => n.EmployeeId == id)
                    .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                    .Skip(KeepPerUser)
                    .Select(n => (int?)n.Id)
                    .FirstOrDefaultAsync();
                if (cutoff != null)
                {
                    await _context.Notifications
                        .Where(n => n.EmployeeId == id && n.Id <= cutoff)
                        .ExecuteDeleteAsync();
                }
            }
        }
        catch (DbUpdateException ex)
        {
            // A notification must never break the action that sent it
            _logger.LogWarning(ex, "Could not save notification {Key}", messageKey);
        }
    }
}
