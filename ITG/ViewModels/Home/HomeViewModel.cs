using ITG.Models.Entities;
using ITG.Services.Models;

namespace ITG.ViewModels.Home;

public class HomeViewModel
{
    public CareerProgress Progress { get; init; } = new();

    // Latest soft-skills result (null if the assessment was never taken)
    public SkillAssessment? LatestAssessment { get; init; }

    // The assessment before the latest one (null if there is only one)
    public SkillAssessment? PreviousAssessment { get; init; }

    // True if the employee may start the assessment now
    public bool CanTakeAssessment { get; init; }

    // How close the employee is to the next promotion
    public PromotionReadiness Readiness { get; init; } = PromotionReadiness.NotApplicable;

    // The AI development plan (null if none was created yet)
    public PlanView? Plan { get; init; }
}
