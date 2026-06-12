using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Common.Constants;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Access)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(TimesheetConstants.AccessAllowed);
        builder.HasIndex(x => new { x.UserId, x.WeekStart }).IsUnique();
        builder.HasOne(x => x.User)
            .WithMany(x => x.Timesheets)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
