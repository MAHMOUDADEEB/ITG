namespace ITG.Models.Entities;

// A message in the bell menu of the top bar.
// The text is saved as a translation key + values, so it shows in the reader's language.
public class Notification
{
    public int Id { get; set; }

    // Who receives it (employee or admin)
    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    public string MessageKey { get; set; } = string.Empty;

    // Values for the {0}, {1} ... in the message (JSON array)
    public string? Arguments { get; set; }

    // Bootstrap icon name, e.g. "bi-patch-check-fill"
    public string Icon { get; set; } = "bi-bell";

    // Page opened when the notification is clicked
    public string? Url { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
