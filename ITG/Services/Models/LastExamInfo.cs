namespace ITG.Services.Models;

// Summary of the employee's most recent completed exam (Dashboard "Last Exam")
public record LastExamInfo(string ExamTitle, int AttemptNumber, int Score, DateTime CompletedAt);
