using ITG.Constants;

namespace ITG.Services.Models;

// ----- Stored in ExamAttempt.QuestionsJson -----
public record ExamQuestion(string Question, List<string> Options, int CorrectIndex, string? Explanation);

// ----- Exams page -----
public enum ExamCardState
{
    Available,
    InProgress,
    Completed, // latest attempt passed (60% or more)
    Failed     // latest attempt below 60%: can be taken again, not counted as completed
}

public record ExamCard(
    int ExamId,
    string Title,
    string Category,
    string? Description,
    int DurationMinutes,
    int QuestionCount,
    bool ForCurrentRole,   // linked to the employee's current position
    bool ForNextRole,      // linked to the position the employee works towards
    ExamCardState State,
    int CompletedAttempts,
    int? LatestScore,
    int? LatestAttemptId,
    int? LatestAttemptNumber,
    int? InProgressAttemptId)
{
    // An exam counts as completed only when the latest attempt passed
    public bool IsPassed => LatestScore >= AppConstants.ExamPassingScore;
}

public class ExamsOverview
{
    public IReadOnlyList<ExamCard> Exams { get; init; } = Array.Empty<ExamCard>();
    public int TotalCount => Exams.Count;
    public int CompletedCount => Exams.Count(e => e.IsPassed);
    public int AvailableCount => TotalCount - CompletedCount;
    public int Percent => TotalCount == 0 ? 0 : (int)Math.Round(CompletedCount * 100.0 / TotalCount);

    // Average of the latest score of each completed exam
    public int? AverageScore
    {
        get
        {
            var scores = Exams.Where(e => e.LatestScore.HasValue).Select(e => e.LatestScore!.Value).ToList();
            return scores.Count == 0 ? null : (int)Math.Round(scores.Average());
        }
    }
}

// ----- Taking an exam -----
public record ExamToTake(
    int AttemptId,
    string ExamTitle,
    int AttemptNumber,
    DateTime ExpiresAtUtc,
    IReadOnlyList<ExamQuestionView> Questions);

// The question without the correct answer (sent to the browser)
public record ExamQuestionView(string Question, IReadOnlyList<string> Options);

// ----- Result of an attempt -----
public record ExamResultItem(string Question, IReadOnlyList<string> Options, int CorrectIndex, int? ChosenIndex, string? Explanation)
{
    public bool IsCorrect => ChosenIndex == CorrectIndex;
}

public record ExamResult(
    int AttemptId,
    int ExamId,
    string ExamTitle,
    int AttemptNumber,
    int Score,
    int CorrectAnswers,
    int TotalQuestions,
    bool Passed,
    DateTime StartedAt,
    DateTime CompletedAt,
    IReadOnlyList<ExamResultItem> Items);
