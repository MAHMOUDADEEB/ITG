using ITG.Services.Models;

namespace ITG.ViewModels.Dashboard;

public class DashboardViewModel
{
    public CareerProgress Progress { get; init; } = new();
    public LastExamInfo? LastExam { get; init; }
}
