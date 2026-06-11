using Microsoft.EntityFrameworkCore;
using Prm.Common.Enums;
using Prm.Common.Models.Resources;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class UserRepository(AppDbContext _dbContext)
    : CrudBaseRepository<User, int>(_dbContext), IUserRepository
{
    public Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        return DbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == normalized, cancellationToken);
    }

    public Task<User?> GetByIdWithRole(int userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<User?> GetActiveManagerById(int userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Id == userId
                    && x.IsActive
                    && x.RoleId == (int)RoleNameEnum.Manager,
                cancellationToken);

    public async Task<IReadOnlyList<User>> GetUsers(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Role)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByUsername(string username, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.Username == username.Trim(), cancellationToken);

    public Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.Email == email.Trim(), cancellationToken);

    public async Task<bool> IsLastActiveAdmin(User user, CancellationToken cancellationToken)
    {
        var admins = await DbSet
            .Where(activeUser => activeUser.IsActive && activeUser.RoleId == (int)RoleNameEnum.Admin)
            .ToListAsync(cancellationToken);

        return admins.Count == 1 && admins.Any(admin => admin.Id == user.Id);
    }

    public async Task<IReadOnlyList<User>> GetResourceUsers(
        ResourceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(x => x.ResourceStatusHistories)
                .ThenInclude(x => x.ResourceStatusType)
            .Where(x => x.RoleId == (int)RoleNameEnum.Employee)
            .AsQueryable();

        if (!filter.IncludeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var normalizedStatus = filter.Status.Trim().ToUpperInvariant();
            query = query.Where(x => x.ResourceStatusHistories.Any(history =>
                history.EffectiveToUtc == null
                && history.ResourceStatusType.Name == normalizedStatus));
        }

        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            var normalizedDepartment = filter.Department.Trim();
            query = query.Where(x => x.Department == normalizedDepartment);
        }

        return await query
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetResourcePoolUsers(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.UserSkills)
                .ThenInclude(x => x.Skill)
            .Include(x => x.Allocations)
            .Where(x => x.RoleId == (int)RoleNameEnum.Employee && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> GetResourceUsersByManagerUserId(
        int managerUserId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.UserSkills)
                .ThenInclude(x => x.Skill)
            .Include(x => x.Allocations)
            .Where(x =>
                x.ManagerHistories.Any(history =>
                    history.ManagerUserId == managerUserId
                    && history.EffectiveToUtc == null)
                && x.RoleId == (int)RoleNameEnum.Employee
                && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

    public Task<User?> GetResourceUserDetailById(int userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.UserSkills)
                .ThenInclude(x => x.Skill)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);

    public async Task SetManager(int userId, int managerUserId, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var activeHistories = await _dbContext.ResourceManagerHistories
            .Where(history => history.UserId == userId && history.EffectiveToUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var history in activeHistories)
        {
            history.EffectiveToUtc = utcNow;
        }

        await _dbContext.ResourceManagerHistories.AddAsync(
            new ResourceManagerHistory
            {
                UserId = userId,
                ManagerUserId = managerUserId,
                EffectiveFromUtc = utcNow,
            },
            cancellationToken);
    }

    public async Task SetCurrentResourceStatus(
        int userId,
        int resourceStatusTypeId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var activeHistory = await _dbContext.ResourceStatusHistories
            .Where(history => history.UserId == userId && history.EffectiveToUtc == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeHistory != null && activeHistory.ResourceStatusTypeId == resourceStatusTypeId)
        {
            return;
        }

        await _dbContext.ResourceStatusHistories.AddAsync(
            new ResourceStatusHistory
            {
                UserId = userId,
                ResourceStatusTypeId = resourceStatusTypeId,
                EffectiveFromUtc = utcNow,
            },
            cancellationToken);
    }
}
