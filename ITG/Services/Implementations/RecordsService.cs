using ITG.Constants;
using ITG.Data;
using ITG.Models.Enums;
using ITG.Services.Interfaces;
using ITG.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ITG.Services.Implementations;

// Builds the employee's Records page: exams, verified certificates and skills score
public class RecordsService : IRecordsService
{
    private readonly AppDbContext _context;

    public RecordsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeRecords> GetAsync(string employeeId)
    {
        // ----- Verified certificates (newest first) -----
        var certificates = (await _context.EmployeeCertificates
                .AsNoTracking()
                .Where(c => c.EmployeeId == employeeId && c.Status == VerificationStatus.Verified)
                .Select(c => new { c.CertificateId, c.Certificate.Title, c.IssuedOn, c.ReviewedAt, c.SubmittedAt })
                .ToListAsync())
            // The issue date is the one on the certificate; older uploads may not have it
            .Select(c => new RecordCertificate(c.CertificateId, c.Title, c.IssuedOn ?? (c.ReviewedAt ?? c.SubmittedAt).ToLocalFromUtc()))
            .OrderByDescending(c => c.IssuedOn)
            .ToList();

        // ----- Completed exam attempts, one row per exam -----
        var attempts = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Status == ExamAttemptStatus.Completed && a.Score != null)
            .Select(a => new { a.Id, a.ExamId, a.Exam.Title, Score = a.Score!.Value, a.CompletedAt, a.StartedAt })
            .ToListAsync();

        var exams = attempts
            .GroupBy(a => new { a.ExamId, a.Title })
            .Select(g =>
            {
                var latest = g.OrderByDescending(a => a.CompletedAt ?? a.StartedAt).First();
                return new
                {
                    Row = new RecordExam(g.Key.ExamId, g.Key.Title, g.Count(), latest.Score,
                        latest.Score >= AppConstants.ExamPassingScore, latest.Id),
                    LastDate = latest.CompletedAt ?? latest.StartedAt
                };
            })
            .OrderByDescending(x => x.LastDate)
            .Select(x => x.Row)
            .ToList();

        // ----- Skills score: average of the four skills in the latest assessment -----
        var skillsScore = await _context.SkillAssessments
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => (int?)s.OverallScore)
            .FirstOrDefaultAsync();

        return new EmployeeRecords
        {
            ExamsPassed = exams.Count(e => e.Passed),
            CertificatesEarned = certificates.Count,
            SkillsScore = skillsScore,
            Certificates = certificates,
            Exams = exams
        };
    }
}
