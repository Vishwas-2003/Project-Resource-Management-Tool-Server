using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Employees;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class EmployeeService(
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IMapper mapper) : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<int> Add(AddEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRoleAndEmployee(request.UserId, cancellationToken);
        ValidateUserForEmployeeProfile(user);

        if (await _employeeRepository.ExistsByUserId(request.UserId, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Employees.ProfileAlreadyExists);
        }

        var employee = _mapper.Map<Employee>(request);
        employee.Status = user!.RoleId == (int)RoleNameEnum.Manager
            ? null
            : EmployeeConstants.StatusBench;

        await _employeeRepository.Add(employee, cancellationToken);
        await _employeeRepository.SaveChanges(cancellationToken);

        return employee.Id;
    }

    public async Task<EmployeeListResult> GetEmployees(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetEmployees(filter, cancellationToken);
        var summaries = _mapper.Map<IReadOnlyList<EmployeeSummary>>(employees);

        return new EmployeeListResult
        {
            Employees = summaries,
            Total = summaries.Count,
            Allocated = summaries.Count(x => x.Status == EmployeeConstants.StatusAllocated),
            Bench = summaries.Count(x => x.Status == EmployeeConstants.StatusBench),
        };
    }

    public async Task<bool> Update(
        int employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrow(employeeId, cancellationToken);

        if (!employee.User.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Employees.AlreadyDeactivated);
        }

        _mapper.Map(request, employee);
        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<bool> Deactivate(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrow(employeeId, cancellationToken);

        if (!employee.User.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Employees.AlreadyDeactivated);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocations = employee.Allocations.Where(x => x.ToDate >= today).ToList();

        foreach (var allocation in activeAllocations)
        {
            allocation.ToDate = today;
        }

        if (employee.User.RoleId == (int)RoleNameEnum.Employee)
        {
            employee.Status = EmployeeConstants.StatusBench;
        }

        employee.User.IsActive = false;

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChanges(cancellationToken);

        return true;
    }

    private async Task<Employee> GetEmployeeOrThrow(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetById(employeeId, cancellationToken);
        if (employee is null)
        {
            throw new KeyNotFoundException(AppConstants.Employees.NotFound);
        }

        return employee;
    }

    private static void ValidateUserForEmployeeProfile(User? user)
    {
        if (user is null)
        {
            throw new KeyNotFoundException(AppConstants.Employees.UserNotFound);
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Employees.UserInactive);
        }

        if (user.RoleId is not (int)RoleNameEnum.Employee and not (int)RoleNameEnum.Manager)
        {
            throw new InvalidOperationException(AppConstants.Employees.InvalidRoleForEmployee);
        }

        if (user.Employee is not null)
        {
            throw new InvalidOperationException(AppConstants.Employees.ProfileAlreadyExists);
        }
    }
}
