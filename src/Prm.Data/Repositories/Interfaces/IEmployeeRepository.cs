using Prm.Common.Models.Employees;
using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IEmployeeRepository : ICrudBaseRepository<Employee, int>
{
    Task<bool> ExistsByUserId(int userId, CancellationToken cancellationToken = default);
    Task<Employee?> GetEmployeeByUserId(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetEmployees(EmployeeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetResourcePoolEmployees(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetEmployeesByManagerUserId(
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<Employee?> GetEmployeeDetailById(int employeeId, CancellationToken cancellationToken = default);
}
