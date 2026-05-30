using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class TimesheetActivityTagConfiguration : IEntityTypeConfiguration<TimesheetActivityTag>
{
    public void Configure(EntityTypeBuilder<TimesheetActivityTag> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TimesheetEntryId, x.ActivityTagId }).IsUnique();
        builder.HasOne(x => x.TimesheetEntry)
            .WithMany(x => x.ActivityTags)
            .HasForeignKey(x => x.TimesheetEntryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ActivityTag)
            .WithMany(x => x.TimesheetActivityTags)
            .HasForeignKey(x => x.ActivityTagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
