using ITG.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ITG.Data;

// IdentityDbContext adds the Identity tables (roles, logins, ...) automatically
public class AppDbContext : IdentityDbContext<Employee>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<CareerPath> CareerPaths => Set<CareerPath>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<PositionCertificate> PositionCertificates => Set<PositionCertificate>();
    public DbSet<EmployeeCertificate> EmployeeCertificates => Set<EmployeeCertificate>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<SkillAssessment> SkillAssessments => Set<SkillAssessment>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<ExamPosition> ExamPositions => Set<ExamPosition>();
    public DbSet<EmployeeCareerPathOption> EmployeeCareerPathOptions => Set<EmployeeCareerPathOption>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DevelopmentPlan> DevelopmentPlans => Set<DevelopmentPlan>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must be called first so Identity configures its own tables
        base.OnModelCreating(builder);

        // Loads every configuration class from the Data/Configurations folder
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
