using Benefits.Application.Employees;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benefits.Api.Tests;

public class GetEmployeeServiceTests
{
    private readonly FakeEmployeeRepository _repository = new();

    private GetEmployeeService CreateService() =>
        new(_repository, NullLogger<GetEmployeeService>.Instance);

    /// <summary>
    /// Adds one real employee through the AddStarterService using the same
    /// repository, ensuring the fake's generated employee numbers exist.
    /// </summary>
    private async Task<int> SaveOneEmployeeAsync()
    {
        var service = new AddStarterService(_repository, new EmployeeNoGenerator(), NullLogger<AddStarterService>.Instance);

        var result = await service.AddStarterAsync(new AddStarterRequest
        {
            Firstname = "Freddie",
            Surname = "Mercury",
            Email = "get.employee@example.test",
        });

        Assert.True(result.IsSuccess);
        return result.Employee!.EmployeeNo;
    }

    [Fact]
    public async Task GetEmployee_ExistingNumber_ReturnsEmployee()
    {
        var employeeNo = await SaveOneEmployeeAsync();
        var service = CreateService();

        var result = await service.GetEmployeeAsync(employeeNo);

        Assert.True(result.IsSuccess);
        Assert.Equal(employeeNo, result.Employee!.EmployeeNo);
        Assert.Equal("get.employee@example.test", result.Employee.Email);
        Assert.Equal("Freddie", result.Employee.Firstname);
    }

    [Fact]
    public async Task GetEmployee_UnknownNumber_ReturnsNotFound()
    {
        var service = CreateService();

        var result = await service.GetEmployeeAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetEmployeeError.NotFound, result.Error);
        Assert.Null(result.Employee);
    }
}
