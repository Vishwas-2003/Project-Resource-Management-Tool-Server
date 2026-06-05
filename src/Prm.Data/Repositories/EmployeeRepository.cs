using Microsoft.EntityFrameworkCore;
using Prm.Common.Enums;
using Prm.Common.Models.Employees;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class EmployeeRepository(AppDbContext _dbContext)
    : CrudBaseRepository<Employee, int>(_dbContext), IEmployeeRepository
{
    public override Task<Employee?> GetById(int employeeId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.User)
                .ThenInclude(x => x.Role)
            .Include(x => x.EmployeeSkills)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken);

    public Task<bool> ExistsByUserId(int userId, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Employee>> GetEmployees(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(x => x.User)
            .Where(x => x.User.RoleId == (int)RoleNameEnum.Employee)
            .AsQueryable();

        if (!filter.IncludeInactive)
        {
            query = query.Where(x => x.User.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var normalizedStatus = filter.Status.Trim().ToUpperInvariant();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            var normalizedDepartment = filter.Department.Trim();
            query = query.Where(x => x.Department == normalizedDepartment);
        }

        return await query
            .OrderBy(x => x.User.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetManagerById(int employeeId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Id == employeeId
                    && x.User.RoleId == (int)RoleNameEnum.Manager
                    && x.User.IsActive,
                cancellationToken);

    public Task<Employee?> GetEmployeeByUserId(int userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.User)
            .FirstOrDefaultAsync(employee => employee.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Employee>> GetResourcePoolEmployees(
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.User)
            .Include(x => x.EmployeeSkills)
                .ThenInclude(x => x.Skill)
            .Include(x => x.Allocations)
            .Where(x => x.User.RoleId == (int)RoleNameEnum.Employee && x.User.IsActive)
            .OrderBy(x => x.User.FullName)
            .ToListAsync(cancellationToken);

    public Task<Employee?> GetEmployeeDetailById(int employeeId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.User)
            .Include(x => x.EmployeeSkills)
                .ThenInclude(x => x.Skill)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == employeeId && x.User.IsActive, cancellationToken);
}
