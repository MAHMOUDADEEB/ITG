using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(a => new { a.EmployeeId, a.ExamId });

        builder.HasOne(a => a.Employee)
               .WithMany(e => e.ExamAttempts)
               .HasForeignKey(a => a.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        // Exams with attempts are deactivated, never deleted
        builder.HasOne(a => a.Exam)
               .WithMany(e => e.Attempts)
               .HasForeignKey(a => a.ExamId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
