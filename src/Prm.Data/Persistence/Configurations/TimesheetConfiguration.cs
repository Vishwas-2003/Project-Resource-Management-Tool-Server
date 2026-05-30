using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.HasKey(x => x.TimesheetId);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.EmployeeId, x.WeekStart }).IsUnique();
        builder.HasOne(x => x.Employee)
            .WithMany(x => x.Timesheets)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
