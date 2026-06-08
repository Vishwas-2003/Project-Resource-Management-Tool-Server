using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Models;
using Prm.Common.Models.Employees;
using Prm.Common.Models.Manager;

namespace Prm.Api.Tests.Controllers;

public class EmployeeControllerTests
{
    private readonly Mock<IEmployeeService> _employeeService = new();

    [Fact]
    public async Task GetEmployees_WhenEmployeesExist_ReturnsOk()
    {
        var response = new EmployeeListResult
        {
            Total = 1,
            Employees = [new EmployeeSummary { Id = 1, FullName = "Jane Doe" }],
        };

        _employeeService
            .Setup(x => x.GetEmployees(It.IsAny<EmployeeFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new EmployeeController(_employeeService.Object);
        var result = await sut.GetEmployees(new EmployeeFilter(), CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<EmployeeListResult>(result);
        Assert.Equal(1, value.Total);
    }

    [Fact]
    public async Task AssignManager_WhenValid_ReturnsOk()
    {
        _employeeService
            .Setup(x => x.AssignManager(It.IsAny<AssignManagerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new EmployeeController(_employeeService.Object);
        var result = await sut.AssignManager(
            new AssignManagerRequest
            {
                EmployeeUserId = 1,
                ManagerUserId = 10,
                Department = "Engineering",
                Designation = "Developer",
            },
            CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<UpdatedResponse>(result);
        Assert.True(value.Updated);
    }

    [Fact]
    public async Task Update_WhenValid_ReturnsOk()
    {
        _employeeService
            .Setup(x => x.Update(1, It.IsAny<UpdateEmployeeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new EmployeeController(_employeeService.Object);
        var result = await sut.Update(
            1,
            new UpdateEmployeeRequest { Department = "Engineering", Designation = "Developer" },
            CancellationToken.None);

        Assert.True(ControllerTestHelper.AssertOkValue<UpdatedResponse>(result).Updated);
    }

    [Fact]
    public async Task Deactivate_WhenValid_ReturnsOk()
    {
        _employeeService
            .Setup(x => x.Deactivate(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new EmployeeController(_employeeService.Object);
        var result = await sut.Deactivate(1, CancellationToken.None);

        Assert.True(ControllerTestHelper.AssertOkValue<UpdatedResponse>(result).Updated);
    }

    [Fact]
    public async Task GetDetail_WhenEmployeeNotFound_Returns404()
    {
        _employeeService
            .Setup(x => x.GetDetail(99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(AppConstants.Employees.NotFound));

        var sut = new EmployeeController(_employeeService.Object);
        var result = await sut.GetDetail(99, CancellationToken.None);

        var error = ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound);
        Assert.Equal(AppConstants.Employees.NotFound, error.Message);
    }

    [Fact]
    public async Task GetUtilization_WhenValid_ReturnsOk()
    {
        var response = new EmployeeUtilizationResponse { EmployeeId = 1, UtilizationPercent = 80 };

        _employeeService
            .Setup(x => x.GetUtilization(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new EmployeeController(_employeeService.Object);
        var result = await sut.GetUtilization(1, CancellationToken.None);

        Assert.Equal(80, ControllerTestHelper.AssertOkValue<EmployeeUtilizationResponse>(result).UtilizationPercent);
    }
}
