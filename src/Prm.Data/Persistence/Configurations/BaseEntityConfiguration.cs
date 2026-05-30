using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

internal static class BaseEntityConfiguration
{
    internal static void ApplyBaseEntityProperties(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(clrType => typeof(BaseEntity).IsAssignableFrom(clrType) && clrType != typeof(BaseEntity)))
        {
            ConfigureEntity(modelBuilder.Entity(entityType));
        }
    }

    private static void ConfigureEntity(EntityTypeBuilder entityBuilder)
    {
        entityBuilder.Property(nameof(BaseEntity.CreatedAtUtc)).IsRequired();
        entityBuilder.Property(nameof(BaseEntity.ModifiedAtUtc));
        entityBuilder.Property(nameof(BaseEntity.CreatedByUserId));
        entityBuilder.Property(nameof(BaseEntity.ModifiedByUserId));

        entityBuilder
            .HasOne(typeof(User), nameof(BaseEntity.CreatedByUser))
            .WithMany()
            .HasForeignKey(nameof(BaseEntity.CreatedByUserId))
            .OnDelete(DeleteBehavior.Restrict);

        entityBuilder
            .HasOne(typeof(User), nameof(BaseEntity.ModifiedByUser))
            .WithMany()
            .HasForeignKey(nameof(BaseEntity.ModifiedByUserId))
            .OnDelete(DeleteBehavior.Restrict);
    }
}
