using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>
{
    public void Configure(EntityTypeBuilder<TimesheetEntry> builder)
    {
        builder.HasKey(x => x.EntryId);
        builder.HasOne(x => x.Timesheet)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Project)
            .WithMany(x => x.TimesheetEntries)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
