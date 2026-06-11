using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class ResourceStatusHistoryConfiguration : IEntityTypeConfiguration<ResourceStatusHistory>
{
    public void Configure(EntityTypeBuilder<ResourceStatusHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.User)
            .WithMany(x => x.ResourceStatusHistories)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ResourceStatusType)
            .WithMany(x => x.ResourceStatusHistories)
            .HasForeignKey(x => x.ResourceStatusTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ResourceStatusTypeId);
    }
}
