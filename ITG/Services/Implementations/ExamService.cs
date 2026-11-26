using ITG.Constants;
using ITG.Data;
using ITG.Models.Entities;
using ITG.Models.Enums;
using ITG.Services.Ai;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

// AI self-assessment exams: questions are written by the AI when an attempt starts,
// graded by the server, and kept so the employee can review the result.
public class ExamService : IExamService
{
    // Extra seconds after the timer ends (network delay when the page auto-submits)
    private const int GraceSeconds = 60;

    private readonly AppDbContext _context;
    private readonly IAiClient _ai;
    private readonly ICareerService _careerService;

    public ExamService(AppDbContext context, IAiClient ai, ICareerService careerService)
    {
        _context = context;
        _ai = ai;
        _careerService = careerService;
    }

    public Task<LastExamInfo?> GetLastCompletedAsync(string employeeId) =>
        _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Status == ExamAttemptStatus.Completed && a.Score != null)
            .OrderByDescending(a => a.CompletedAt)
            .Select(a => new LastExamInfo(a.Exam.Title, a.AttemptNumber, a.Score!.Value, a.CompletedAt!.Value))
            .FirstOrDefaultAsync();

    public async Task<ExamsOverview> GetOverviewAsync(string employeeId)
    {
        await CloseExpiredAttemptsAsync(employeeId);

        // Only the exams of the employee's current position and of the position they work towards
        var progress = await _careerService.GetProgressAsync(employeeId);
        var currentId = progress.CurrentPositionId;
        var targetId = progress.TargetPositionId;

        var exams = await _context.Exams
            .AsNoTracking()
            .Where(e => e.IsActive && e.ExamPositions.Any(ep => ep.PositionId == currentId || ep.PositionId == targetId))
            .OrderBy(e => e.Title)
            .Select(e => new
            {
                e.Id, e.Title, e.Category, e.Description, e.DurationMinutes, e.QuestionCount,
                ForCurrent = e.ExamPositions.Any(ep => ep.PositionId == currentId),
                ForNext = e.ExamPositions.Any(ep => ep.PositionId == targetId)
            })
            .ToListAsync();

        var attempts = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .Select(a => new { a.Id, a.ExamId, a.AttemptNumber, a.Status, a.Score, a.CompletedAt, a.StartedAt })
            .ToListAsync();

        var cards = exams.Select(e =>
        {
            var forExam = attempts.Where(a => a.ExamId == e.Id).ToList();
            var completed = forExam.Where(a => a.Status == ExamAttemptStatus.Completed).ToList();
            var latest = completed.OrderByDescending(a => a.CompletedAt).FirstOrDefault();
            var open = forExam.FirstOrDefault(a => a.Status == ExamAttemptStatus.InProgress);

            var state = open != null ? ExamCardState.InProgress
                : latest == null ? ExamCardState.Available
                : latest.Score >= AppConstants.ExamPassingScore ? ExamCardState.Completed
                : ExamCardState.Failed;

            return new ExamCard(e.Id, e.Title, e.Category, e.Description, e.DurationMinutes, e.QuestionCount,
                e.ForCurrent, e.ForNext,
                state, completed.Count, latest?.Score, latest?.Id, latest?.AttemptNumber, open?.Id);
        })
        // Exams for the next role first
        .OrderByDescending(c => c.ForNextRole)
        .ThenBy(c => c.Title)
        .ToList();

        return new ExamsOverview { Exams = cards };
    }

    public async Task<int> StartAttemptAsync(string employeeId, int examId, string cultureName)
    {
        var exam = await _context.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.Id == examId && e.IsActive)
                   ?? throw new InvalidOperationException("Exam not found.");

        // An employee can only take the exams of their current or next position
        var progress = await _careerService.GetProgressAsync(employeeId);
        var allowed = await _context.ExamPositions.AnyAsync(ep => ep.ExamId == examId
            && (ep.PositionId == progress.CurrentPositionId || ep.PositionId == progress.TargetPositionId));
        if (!allowed)
        {
            throw new InvalidOperationException("Exam not found.");
        }

        // Continue an attempt that is still open instead of creating a new one
        await CloseExpiredAttemptsAsync(employeeId);
        var open = await _context.ExamAttempts
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.ExamId == examId && a.Status == ExamAttemptStatus.InProgress);
        if (open != null)
        {
            return open.Id;
        }

        var questions = await GenerateQuestionsAsync(exam, cultureName);

        var attemptNumber = await _context.ExamAttempts.CountAsync(a => a.EmployeeId == employeeId && a.ExamId == examId) + 1;
        var now = DateTime.UtcNow;

        var attempt = new ExamAttempt
        {
            ExamId = exam.Id,
            EmployeeId = employeeId,
            AttemptNumber = attemptNumber,
            Status = ExamAttemptStatus.InProgress,
            StartedAt = now,
            ExpiresAt = now.AddMinutes(exam.DurationMinutes),
            TotalQuestions = questions.Count,
            QuestionsJson = AiJson.Serialize(questions)
        };

        _context.ExamAttempts.Add(attempt);
        await _context.SaveChangesAsync();
        return attempt.Id;
    }

    public async Task<ExamToTake?> GetAttemptToTakeAsync(int attemptId, string employeeId)
    {
        var attempt = await _context.ExamAttempts
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.EmployeeId == employeeId);

        if (attempt == null || attempt.Status != ExamAttemptStatus.InProgress)
        {
            return null;
        }

        if (DateTime.UtcNow > attempt.ExpiresAt.AddSeconds(GraceSeconds))
        {
            Grade(attempt, Array.Empty<int?>());
            await _context.SaveChangesAsync();
            return null;
        }

        var questions = AiJson.Deserialize<List<ExamQuestion>>(attempt.QuestionsJson) ?? new List<ExamQuestion>();

        return new ExamToTake(
            attempt.Id,
            attempt.Exam.Title,
            attempt.AttemptNumber,
            DateTime.SpecifyKind(attempt.ExpiresAt, DateTimeKind.Utc),
            questions.Select(q => new ExamQuestionView(q.Question, q.Options)).ToList());
    }

    public async Task<bool> SubmitAsync(int attemptId, string employeeId, IReadOnlyList<int?> answers)
    {
        var attempt = await _context.ExamAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.EmployeeId == employeeId);

        if (attempt == null || attempt.Status != ExamAttemptStatus.InProgress)
        {
            return false;
        }

        // Answers sent after the time limit are ignored
        var inTime = DateTime.UtcNow <= attempt.ExpiresAt.AddSeconds(GraceSeconds);
        Grade(attempt, inTime ? answers : Array.Empty<int?>());

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ExamResult?> GetResultAsync(int attemptId, string employeeId)
    {
        var attempt = await _context.ExamAttempts
            .AsNoTracking()
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.EmployeeId == employeeId);

        if (attempt == null || attempt.Status != ExamAttemptStatus.Completed || attempt.Score == null)
        {
            return null;
        }

        var questions = AiJson.Deserialize<List<ExamQuestion>>(attempt.QuestionsJson) ?? new List<ExamQuestion>();
        var chosen = AiJson.Deserialize<List<int?>>(attempt.AnswersJson) ?? new List<int?>();

        var items = questions
            .Select((q, i) => new ExamResultItem(q.Question, q.Options, q.CorrectIndex,
                i < chosen.Count ? chosen[i] : null, q.Explanation))
            .ToList();

        return new ExamResult(
            attempt.Id, attempt.ExamId, attempt.Exam.Title, attempt.AttemptNumber,
            attempt.Score.Value, attempt.CorrectAnswers ?? 0, attempt.TotalQuestions,
            attempt.Score.Value >= AppConstants.ExamPassingScore,
            attempt.StartedAt, attempt.CompletedAt ?? attempt.StartedAt, items);
    }

    // ---------------- Helpers ----------------

    private static void Grade(ExamAttempt attempt, IReadOnlyList<int?> answers)
    {
        var questions = AiJson.Deserialize<List<ExamQuestion>>(attempt.QuestionsJson) ?? new List<ExamQuestion>();
        var chosen = questions.Select((_, i) => i < answers.Count ? answers[i] : null).ToList();
        var correct = questions.Where((q, i) => chosen[i] == q.CorrectIndex).Count();

        attempt.AnswersJson = AiJson.Serialize(chosen);
        attempt.CorrectAnswers = correct;
        attempt.TotalQuestions = questions.Count;
        attempt.Score = questions.Count == 0 ? 0 : (int)Math.Round(correct * 100.0 / questions.Count);
        attempt.Status = ExamAttemptStatus.Completed;
        attempt.CompletedAt = DateTime.UtcNow;
    }

    // Attempts whose time ran out are closed with the answers they have (none)
    private async Task CloseExpiredAttemptsAsync(string employeeId)
    {
        var limit = DateTime.UtcNow.AddSeconds(-GraceSeconds);
        var expired = await _context.ExamAttempts
            .Where(a => a.EmployeeId == employeeId && a.Status == ExamAttemptStatus.InProgress && a.ExpiresAt < limit)
            .ToListAsync();

        foreach (var attempt in expired)
        {
            Grade(attempt, Array.Empty<int?>());
        }

        if (expired.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task<List<ExamQuestion>> GenerateQuestionsAsync(Exam exam, string cultureName)
    {
        var language = AiJson.LanguageName(cultureName);

        var system = $$"""
            You write exams for ITG, a technology company's employee learning platform.
            Write exactly {{exam.QuestionCount}} multiple-choice questions that test an employee's knowledge of "{{exam.Title}}".
            Topic details: {{exam.Description ?? exam.Title}}
            Mix easy, medium and hard questions. Each question has exactly 4 options and exactly one correct answer.
            Keep questions and options short and clear. Write everything in {{language}}.
            Answer with JSON only, in this exact shape:
            {"questions":[{"question":"...","options":["...","...","...","..."],"correctIndex":0,"explanation":"one short sentence"}]}
            """;

        var text = await _ai.GenerateAsync(system,
            new[] { new AiMessage(true, $"Create the {exam.Title} exam now.") }, json: true);

        var generated = AiJson.Parse<GeneratedExam>(text);

        var valid = (generated.Questions ?? new List<ExamQuestion>())
            .Where(q => !string.IsNullOrWhiteSpace(q.Question)
                        && q.Options is { Count: 4 }
                        && q.Options.All(o => !string.IsNullOrWhiteSpace(o))
                        && q.CorrectIndex is >= 0 and <= 3)
            .Take(exam.QuestionCount)
            .Select(Shuffle)
            .ToList();

        // A few questions may be dropped if the AI wrote them badly, but we need most of them
        if (valid.Count < Math.Min(5, exam.QuestionCount))
        {
            throw new AiUnavailableException(AiFailure.BadResponse, text);
        }

        return valid;
    }

    // The AI tends to put the right answer first, so the options are shuffled
    private static ExamQuestion Shuffle(ExamQuestion question)
    {
        var order = Enumerable.Range(0, question.Options.Count).OrderBy(_ => Random.Shared.Next()).ToList();
        var options = order.Select(i => question.Options[i].Trim()).ToList();
        return question with
        {
            Question = question.Question.Trim(),
            Options = options,
            CorrectIndex = order.IndexOf(question.CorrectIndex)
        };
    }

    private sealed record GeneratedExam(List<ExamQuestion>? Questions);
}
