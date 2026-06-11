using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class ResourceManagerHistoryConfiguration : IEntityTypeConfiguration<ResourceManagerHistory>
{
    public void Configure(EntityTypeBuilder<ResourceManagerHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.User)
            .WithMany(x => x.ManagerHistories)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ManagerUser)
            .WithMany(x => x.SubordinateManagerHistories)
            .HasForeignKey(x => x.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ManagerUserId);
    }
}
