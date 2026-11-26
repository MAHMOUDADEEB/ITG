using ITG.Models.Entities;
using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface ISkillAssessmentService
{
    Task<SkillAssessment?> GetLatestAsync(string employeeId);

    // The assessment before the latest one (to compare the two)
    Task<SkillAssessment?> GetPreviousAsync(string employeeId);

    // True if the employee never took the assessment, or an admin re-opened it
    Task<bool> CanTakeAsync(string employeeId);

    // Asks the AI for scenario questions (or returns the ones already prepared)
    Task<IReadOnlyList<AssessmentQuestionView>> StartAsync(string employeeId, string cultureName);

    // Scores the answers, saves the result and locks the assessment again
    Task<ServiceResult> SubmitAsync(string employeeId, IReadOnlyList<int?> answers, string cultureName);
}
