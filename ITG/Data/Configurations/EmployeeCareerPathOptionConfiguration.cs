using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class EmployeeCareerPathOptionConfiguration : IEntityTypeConfiguration<EmployeeCareerPathOption>
{
    public void Configure(EntityTypeBuilder<EmployeeCareerPathOption> builder)
    {
        builder.ToTable("EmployeeCareerPathOptions");

        // Composite primary key: a path is opened once for an employee
        builder.HasKey(o => new { o.EmployeeId, o.CareerPathId });

        builder.HasOne(o => o.Employee)
               .WithMany(e => e.CareerPathOptions)
               .HasForeignKey(o => o.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        // A path that is opened for someone cannot be deleted by mistake
        // (the admin page removes these rows first)
        builder.HasOne(o => o.CareerPath)
               .WithMany()
               .HasForeignKey(o => o.CareerPathId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
