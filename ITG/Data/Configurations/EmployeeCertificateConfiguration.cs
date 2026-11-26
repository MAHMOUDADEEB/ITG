using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class EmployeeCertificateConfiguration : IEntityTypeConfiguration<EmployeeCertificate>
{
    public void Configure(EntityTypeBuilder<EmployeeCertificate> builder)
    {
        builder.Property(c => c.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(c => c.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(c => c.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(c => c.RejectionReason).HasMaxLength(500);

        // Store enum as readable text ("Pending") instead of a number
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => new { c.EmployeeId, c.CertificateId });

        // Deleting an employee deletes their uploads
        builder.HasOne(c => c.Employee)
               .WithMany(e => e.Certificates)
               .HasForeignKey(c => c.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        // Certificates with uploads are deactivated, never deleted
        builder.HasOne(c => c.Certificate)
               .WithMany(ce => ce.Submissions)
               .HasForeignKey(c => c.CertificateId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ReviewedBy)
               .WithMany()
               .HasForeignKey(c => c.ReviewedById)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
