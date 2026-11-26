using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.MessageKey).HasMaxLength(300).IsRequired();
        builder.Property(n => n.Arguments).HasMaxLength(1000);
        builder.Property(n => n.Icon).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Url).HasMaxLength(300);

        // Fast "unread count" and "latest first" for one user
        builder.HasIndex(n => new { n.EmployeeId, n.IsRead, n.CreatedAt });

        builder.HasOne(n => n.Employee)
               .WithMany()
               .HasForeignKey(n => n.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
