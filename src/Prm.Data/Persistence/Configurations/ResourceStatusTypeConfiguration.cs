using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class ResourceStatusTypeConfiguration : IEntityTypeConfiguration<ResourceStatusType>
{
    public void Configure(EntityTypeBuilder<ResourceStatusType> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(SeedData.ResourceStatusTypes);
    }
}
