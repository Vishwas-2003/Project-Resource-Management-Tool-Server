using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class ProjectRiskEmailHistoryConfiguration : IEntityTypeConfiguration<ProjectRiskEmailHistory>
{
    public void Configure(EntityTypeBuilder<ProjectRiskEmailHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SentOnDate).IsRequired();
        builder.Property(x => x.SentAtUtc).IsRequired();
        builder.HasIndex(x => new { x.ProjectId, x.SentOnDate }).IsUnique();
        builder.HasIndex(x => x.ManagerUserId);
        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ManagerUser)
            .WithMany()
            .HasForeignKey(x => x.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
