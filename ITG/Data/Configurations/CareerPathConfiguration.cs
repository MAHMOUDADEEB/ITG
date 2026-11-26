using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class CareerPathConfiguration : IEntityTypeConfiguration<CareerPath>
{
    public void Configure(EntityTypeBuilder<CareerPath> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Name).IsUnique();

        // Self-reference must not cascade (SQL Server rejects cycles)
        builder.HasOne(c => c.ParentCareerPath)
               .WithMany(c => c.ChildCareerPaths)
               .HasForeignKey(c => c.ParentCareerPathId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
