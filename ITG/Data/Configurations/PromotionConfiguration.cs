using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.Property(p => p.Note).HasMaxLength(500);

        builder.HasOne(p => p.Employee)
               .WithMany(e => e.Promotions)
               .HasForeignKey(p => p.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.FromPosition)
               .WithMany()
               .HasForeignKey(p => p.FromPositionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ToPosition)
               .WithMany()
               .HasForeignKey(p => p.ToPositionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PromotedBy)
               .WithMany()
               .HasForeignKey(p => p.PromotedById)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
