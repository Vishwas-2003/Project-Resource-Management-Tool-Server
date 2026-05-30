using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(x => x.EmployeeId);
        builder.Property(x => x.Department).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Designation).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.User)
            .WithOne(x => x.Employee)
            .HasForeignKey<Employee>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
