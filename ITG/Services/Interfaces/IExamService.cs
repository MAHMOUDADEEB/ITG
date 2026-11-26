using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface IExamService
{
    Task<LastExamInfo?> GetLastCompletedAsync(string employeeId);

    Task<ExamsOverview> GetOverviewAsync(string employeeId);

    // Asks the AI for new questions and opens a timed attempt. Returns the attempt id.
    Task<int> StartAttemptAsync(string employeeId, int examId, string cultureName);

    // Null if the attempt is not open anymore (finished or time is up)
    Task<ExamToTake?> GetAttemptToTakeAsync(int attemptId, string employeeId);

    // Grades the answers (index of the chosen option per question; null = not answered)
    Task<bool> SubmitAsync(int attemptId, string employeeId, IReadOnlyList<int?> answers);

    Task<ExamResult?> GetResultAsync(int attemptId, string employeeId);
}
