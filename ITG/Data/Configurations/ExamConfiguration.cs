using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.Property(e => e.Title).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Category).IsRequired().HasMaxLength(50);

        builder.HasIndex(e => e.Title).IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Exams_DurationMinutes", "[DurationMinutes] BETWEEN 5 AND 180");
            t.HasCheckConstraint("CK_Exams_QuestionCount", "[QuestionCount] BETWEEN 5 AND 30");
        });
    }
}
