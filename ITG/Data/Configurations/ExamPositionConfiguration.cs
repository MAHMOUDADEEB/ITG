using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class ExamPositionConfiguration : IEntityTypeConfiguration<ExamPosition>
{
    public void Configure(EntityTypeBuilder<ExamPosition> builder)
    {
        // Composite primary key: an exam is linked once to a position
        builder.HasKey(ep => new { ep.ExamId, ep.PositionId });

        // Deleting an exam or a position only removes the link
        builder.HasOne(ep => ep.Exam)
               .WithMany(e => e.ExamPositions)
               .HasForeignKey(ep => ep.ExamId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ep => ep.Position)
               .WithMany(p => p.ExamPositions)
               .HasForeignKey(ep => ep.PositionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
