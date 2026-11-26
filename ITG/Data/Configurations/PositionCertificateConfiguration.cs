using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class PositionCertificateConfiguration : IEntityTypeConfiguration<PositionCertificate>
{
    public void Configure(EntityTypeBuilder<PositionCertificate> builder)
    {
        // Composite primary key: a certificate appears once in a position's roadmap
        builder.HasKey(pc => new { pc.PositionId, pc.CertificateId });

        builder.HasIndex(pc => new { pc.PositionId, pc.SortOrder });

        builder.HasOne(pc => pc.Position)
               .WithMany(p => p.PositionCertificates)
               .HasForeignKey(pc => pc.PositionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Certificate)
               .WithMany(c => c.PositionCertificates)
               .HasForeignKey(pc => pc.CertificateId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
