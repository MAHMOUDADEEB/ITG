namespace ITG.Services.Models;

// ----- Stored in DevelopmentPlan.ContentJson (written by the AI) -----
public class PlanContent
{
    public string Summary { get; set; } = string.Empty;
    public List<PlanFocus> Focus { get; set; } = new();
    public List<PlanWeek> Weeks { get; set; } = new();
}

// A skill or topic the plan concentrates on, and why
public class PlanFocus
{
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class PlanWeek
{
    public string Title { get; set; } = string.Empty;
    public List<PlanTask> Tasks { get; set; } = new();
}

public class PlanTask
{
    public string Text { get; set; } = string.Empty;

    // certificate | exam | skill | practice (chooses the icon)
    public string Type { get; set; } = "practice";

    // Ticked by the employee
    public bool Done { get; set; }
}

// ----- Shown on the "My Plan" page and the Home card -----
public record PlanView(int Id, DateTime CreatedAt, string Language, PlanContent Content)
{
    public int TotalTasks => Content.Weeks.Sum(w => w.Tasks.Count);
    public int DoneTasks => Content.Weeks.Sum(w => w.Tasks.Count(t => t.Done));
    public int Percent => TotalTasks == 0 ? 0 : (int)Math.Round(DoneTasks * 100.0 / TotalTasks);

    // Week 1 starts the day the plan was created
    public int CurrentWeek => Math.Clamp((int)((DateTime.UtcNow - CreatedAt).TotalDays / 7) + 1, 1, Math.Max(1, Content.Weeks.Count));
}

public record PlanTaskResult(int DoneTasks, int TotalTasks, int Percent);
