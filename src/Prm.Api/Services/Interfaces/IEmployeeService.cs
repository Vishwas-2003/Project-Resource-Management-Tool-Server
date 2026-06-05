using Prm.Common.Models.Employees;
using Prm.Common.Models.Manager;

namespace Prm.Api.Services.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeListResult> GetEmployees(EmployeeFilter filter, CancellationToken cancellationToken = default);
    Task<bool> AssignManager(AssignManagerRequest request, CancellationToken cancellationToken = default);
    Task<bool> Update(int employeeId, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<bool> Deactivate(int employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeDetailResponse> GetDetail(int employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeUtilizationResponse> GetUtilization(int employeeId, CancellationToken cancellationToken = default);
}
