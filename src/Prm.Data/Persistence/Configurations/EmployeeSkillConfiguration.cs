using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prm.Data.Entities;

namespace Prm.Data.Persistence.Configurations;

public class EmployeeSkillConfiguration : IEntityTypeConfiguration<EmployeeSkill>
{
    public void Configure(EntityTypeBuilder<EmployeeSkill> builder)
    {
        builder.HasKey(x => new { x.EmployeeId, x.SkillId });
        builder.Property(x => x.Proficiency).HasMaxLength(50).IsRequired();
        builder.HasOne(x => x.Employee)
            .WithMany(x => x.EmployeeSkills)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Skill)
            .WithMany(x => x.EmployeeSkills)
            .HasForeignKey(x => x.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
