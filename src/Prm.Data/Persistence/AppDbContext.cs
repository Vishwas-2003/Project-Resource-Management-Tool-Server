using Microsoft.EntityFrameworkCore;
using Prm.Data.Audit;
using Prm.Data.Entities;
using Prm.Data.Persistence.Configurations;

namespace Prm.Data.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : this(options, NullCurrentUserService.Instance)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ResourceManagerHistory> ResourceManagerHistories => Set<ResourceManagerHistory>();
    public DbSet<ResourceStatusType> ResourceStatusTypes => Set<ResourceStatusType>();
    public DbSet<ResourceStatusHistory> ResourceStatusHistories => Set<ResourceStatusHistory>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<ActivityTag> ActivityTags => Set<ActivityTag>();
    public DbSet<TimesheetActivityTag> TimesheetActivityTags => Set<TimesheetActivityTag>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<ProjectRiskFlag> ProjectRiskFlags => Set<ProjectRiskFlag>();
    public DbSet<ProjectRiskEmailHistory> ProjectRiskEmailHistories => Set<ProjectRiskEmailHistory>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInfo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        BaseEntityConfiguration.ApplyBaseEntityProperties(modelBuilder);

        modelBuilder.Entity<Role>().HasData(SeedData.Roles);
    }

    private void ApplyAuditInfo()
    {
        var utcNow = DateTime.UtcNow;
        var userId = _currentUserService.GetUserId();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
                entry.Entity.CreatedByUserId ??= userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = utcNow;
                if (userId.HasValue)
                {
                    entry.Entity.ModifiedByUserId = userId;
                }

                entry.Property(nameof(BaseEntity.CreatedAtUtc)).IsModified = false;
                entry.Property(nameof(BaseEntity.CreatedByUserId)).IsModified = false;
            }
        }
    }
}
