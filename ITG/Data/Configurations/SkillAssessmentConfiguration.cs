using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class SkillAssessmentConfiguration : IEntityTypeConfiguration<SkillAssessment>
{
    public void Configure(EntityTypeBuilder<SkillAssessment> builder)
    {
        builder.Property(a => a.Summary).HasMaxLength(2000);

        // The database itself rejects any score outside 0-100
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SkillAssessments_OverallScore", "[OverallScore] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_SkillAssessments_TeamworkScore", "[TeamworkScore] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_SkillAssessments_ProblemSolvingScore", "[ProblemSolvingScore] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_SkillAssessments_CommunicationScore", "[CommunicationScore] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_SkillAssessments_LeadershipScore", "[LeadershipScore] BETWEEN 0 AND 100");
        });

        // Fast lookup of an employee's latest assessment
        builder.HasIndex(a => new { a.EmployeeId, a.CreatedAt });

        builder.HasOne(a => a.Employee)
               .WithMany(e => e.SkillAssessments)
               .HasForeignKey(a => a.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
