using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Value)
            .HasMaxLength(500)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.HasData(SeedData.SystemConfigurations);
    }
}
