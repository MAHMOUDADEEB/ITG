using ITG.Models.Enums;

namespace ITG.Models.Entities;

// One attempt of an employee at an exam
public class ExamAttempt
{
    public int Id { get; set; }

    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    public int AttemptNumber { get; set; }
    public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.InProgress;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Score out of 100 (null while in progress)
    public int? Score { get; set; }
    public int? CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }

    // AI-generated questions (with correct answers) and the employee's answers, as JSON
    public string? QuestionsJson { get; set; }
    public string? AnswersJson { get; set; }
}
