using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class ProjectRiskFlagConfiguration : IEntityTypeConfiguration<ProjectRiskFlag>
{
    public void Configure(EntityTypeBuilder<ProjectRiskFlag> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Outcome).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.HasIndex(x => new { x.ProjectId, x.SortOrder });
        builder.HasOne(x => x.Project)
            .WithMany(x => x.RiskFlags)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
