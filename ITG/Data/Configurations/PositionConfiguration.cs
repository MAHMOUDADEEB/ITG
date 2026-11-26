using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.Property(p => p.Title).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(500);

        // Each level appears only once inside a career path
        builder.HasIndex(p => new { p.CareerPathId, p.Level }).IsUnique();

        builder.HasOne(p => p.CareerPath)
               .WithMany(c => c.Positions)
               .HasForeignKey(p => p.CareerPathId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
