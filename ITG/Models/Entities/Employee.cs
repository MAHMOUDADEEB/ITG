using Microsoft.AspNetCore.Identity;

namespace ITG.Models.Entities;

// Inherits Id, Email, PasswordHash, etc. from IdentityUser
public class Employee : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    // Company employee number shown and searched in the admin dashboard (e.g. EMP-001)
    public string EmployeeNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? CurrentPositionId { get; set; }
    public Position? CurrentPosition { get; set; }

    // At a fork: the path assigned by HR, or chosen by the employee from the paths HR opened.
    // Once set, only HR can change it.
    public int? SelectedCareerPathId { get; set; }
    public CareerPath? SelectedCareerPath { get; set; }

    // At a fork: the paths HR opened for the employee to choose from
    public ICollection<EmployeeCareerPathOption> CareerPathOptions { get; set; } = new List<EmployeeCareerPathOption>();

    // The soft-skills assessment can be taken once; an admin can re-open it
    public bool CanRetakeSkillsAssessment { get; set; }

    public ICollection<EmployeeCertificate> Certificates { get; set; } = new List<EmployeeCertificate>();
    public ICollection<SkillAssessment> SkillAssessments { get; set; } = new List<SkillAssessment>();
    public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
    public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
}
