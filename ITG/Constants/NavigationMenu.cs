namespace ITG.Constants;

// One entry in the sidebar menu
public record NavItem(string Area, string Controller, string Icon, string Text);

// Single source of truth for the sidebar links of each role
public static class NavigationMenu
{
    public static readonly IReadOnlyList<NavItem> EmployeeItems = new List<NavItem>
    {
        new("", "Home",        "bi-house-door-fill", "Home"),
        new("", "Dashboard",   "bi-grid-1x2",        "Dashboard"),
        new("", "Exams",       "bi-card-checklist",  "Exams"),
        new("", "AiAssistant", "bi-stars",           "AI"),
        new("", "Plan",        "bi-journal-check",   "My Plan"),
        new("", "Roadmap",     "bi-signpost-split",  "Roadmap"),
        new("", "Records",     "bi-clipboard-check", "Records")
    };

    public static readonly IReadOnlyList<NavItem> AdminItems = new List<NavItem>
    {
        new("Admin", "Dashboard", "bi-speedometer2", "Admin Dashboard")
    };
}
