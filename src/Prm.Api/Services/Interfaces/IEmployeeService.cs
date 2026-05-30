using Prm.Common.Models.Employees;

namespace Prm.Api.Services.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeListResult> GetEmployees(EmployeeFilter filter, CancellationToken cancellationToken = default);
    Task<int> Add(AddEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<bool> Update(int employeeId, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<bool> Deactivate(int employeeId, CancellationToken cancellationToken = default);
}
