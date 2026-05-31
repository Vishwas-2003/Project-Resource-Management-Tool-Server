using Prm.Common.Models.Employees;
using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IEmployeeRepository : ICrudBaseRepository<Employee, int>
{
    Task<bool> ExistsByUserId(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetEmployees(EmployeeFilter filter, CancellationToken cancellationToken = default);
    Task<Employee?> GetManagerById(int employeeId, CancellationToken cancellationToken = default);
}
