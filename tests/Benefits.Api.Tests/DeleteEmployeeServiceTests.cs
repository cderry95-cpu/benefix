using Benefits.Application.Employees;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benefits.Api.Tests;

public class DeleteEmployeeServiceTests
{
    private readonly FakeEmployeeRepository _repository = new();

    private DeleteEmployeeService CreateService() =>
        new(_repository, NullLogger<DeleteEmployeeService>.Instance);

    private async Task<string> SaveOneEmployeeAsync(string? email = null)
    {
        var addService = new AddStarterService(
            _repository, new EmployeeNoGenerator(), NullLogger<AddStarterService>.Instance);

        var result = await addService.AddStarterAsync(new AddStarterRequest
        {
            Firstname = "Freddie",
            Surname = "Mercury",
            Email = email ?? "delete.me@example.test",
        });

        Assert.True(result.IsSuccess);
        return result.Employee!.EmployeeNo;
    }

    [Fact]
    public async Task DeleteEmployee_ExistingNumber_RemovesEmployee()
    {
        var employeeNo = await SaveOneEmployeeAsync();
        var service = CreateService();

        var result = await service.DeleteEmployeeAsync(employeeNo);

        Assert.True(result.IsSuccess);
        Assert.Null(await _repository.GetByEmployeeNoAsync(employeeNo));
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task DeleteEmployee_UnknownNumber_ReturnsNotFound()
    {
        var service = CreateService();

        var result = await service.DeleteEmployeeAsync("EMP-000001");

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteEmployeeError.NotFound, result.Error);
    }

    [Fact]
    public async Task DeleteEmployee_Whitespace_ReturnsNotFound()
    {
        var service = CreateService();

        var result = await service.DeleteEmployeeAsync("   ");

        Assert.Equal(DeleteEmployeeError.NotFound, result.Error);
    }

    [Fact]
    public async Task DeleteEmployee_AlreadyDeleted_ReturnsNotFound()
    {
        // The API distinguishes "removed by this call" from "nothing to
        // remove": deleting an absent number is a NotFound, not a silent win.
        var employeeNo = await SaveOneEmployeeAsync();
        var service = CreateService();
        await service.DeleteEmployeeAsync(employeeNo);

        var result = await service.DeleteEmployeeAsync(employeeNo);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteEmployeeError.NotFound, result.Error);
    }
}
