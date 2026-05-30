using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        builder.HasKey(x => x.ConfigurationId);
        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ApiKey).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.Provider).IsUnique();
    }
}
