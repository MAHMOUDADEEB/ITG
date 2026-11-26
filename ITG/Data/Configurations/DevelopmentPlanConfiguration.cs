using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class DevelopmentPlanConfiguration : IEntityTypeConfiguration<DevelopmentPlan>
{
    public void Configure(EntityTypeBuilder<DevelopmentPlan> builder)
    {
        builder.Property(p => p.ContentJson).IsRequired();
        builder.Property(p => p.Language).HasMaxLength(10).IsRequired();

        builder.HasIndex(p => new { p.EmployeeId, p.CreatedAt });

        builder.HasOne(p => p.Employee)
               .WithMany()
               .HasForeignKey(p => p.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
