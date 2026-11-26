using ITG.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITG.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        // Rename Identity's default "AspNetUsers" table
        builder.ToTable("Employees");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EmployeeNumber).IsRequired().HasMaxLength(20);

        builder.HasIndex(e => e.EmployeeNumber).IsUnique();

        // A department or position that still has employees cannot be deleted
        builder.HasOne(e => e.Department)
               .WithMany(d => d.Employees)
               .HasForeignKey(e => e.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CurrentPosition)
               .WithMany(p => p.Employees)
               .HasForeignKey(e => e.CurrentPositionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SelectedCareerPath)
               .WithMany()
               .HasForeignKey(e => e.SelectedCareerPathId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
