namespace ITG.Services.Models;

// A verified certificate shown on the Records page
public record RecordCertificate(int CertificateId, string Title, DateTime IssuedOn);

// One row of the "Exams Records" table (the final score is the latest completed attempt)
public record RecordExam(int ExamId, string Title, int Attempts, int FinalScore, bool Passed, int LastAttemptId);

// Everything the Records page shows for one employee
public class EmployeeRecords
{
    public int ExamsPassed { get; init; }
    public int CertificatesEarned { get; init; }

    // Average of the four soft skills (null = assessment not taken yet)
    public int? SkillsScore { get; init; }

    public IReadOnlyList<RecordCertificate> Certificates { get; init; } = Array.Empty<RecordCertificate>();
    public IReadOnlyList<RecordExam> Exams { get; init; } = Array.Empty<RecordExam>();
}
