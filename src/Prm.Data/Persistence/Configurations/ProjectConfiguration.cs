using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(x => x.ProjectId);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasOne(x => x.ManagerEmployee)
            .WithMany(x => x.ManagedProjects)
            .HasForeignKey(x => x.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
