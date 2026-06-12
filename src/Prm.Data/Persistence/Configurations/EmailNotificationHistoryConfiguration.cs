using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Common.Enums;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class EmailNotificationHistoryConfiguration : IEntityTypeConfiguration<EmailNotificationHistory>
{
    public void Configure(EntityTypeBuilder<EmailNotificationHistory> builder)
    {
        builder.ToTable("EmailNotificationHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmailTypeId).IsRequired();
        builder.Property(x => x.EntityId);
        builder.Property(x => x.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SentOnDate).IsRequired();
        builder.Property(x => x.SentAtUtc).IsRequired();
        builder.HasIndex(x => new { x.EmailTypeId, x.EntityId, x.SentOnDate })
            .IsUnique()
            .HasFilter($"[{nameof(EmailNotificationHistory.EmailTypeId)}] = {(int)EmailNotificationTypeEnum.ProjectRisk}");
        builder.HasIndex(x => new { x.EmailTypeId, x.UserId, x.SentOnDate })
            .IsUnique()
            .HasFilter($"[{nameof(EmailNotificationHistory.EmailTypeId)}] = {(int)EmailNotificationTypeEnum.MissedTimeSheet}");
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.EntityId);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
