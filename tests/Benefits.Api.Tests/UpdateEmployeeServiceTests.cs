using Benefits.Application.Employees;
using Benefits.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benefits.Api.Tests;

public class UpdateEmployeeServiceTests
{
    private readonly FakeEmployeeRepository _repository = new();

    private UpdateEmployeeService CreateService() =>
        new(_repository, NullLogger<UpdateEmployeeService>.Instance);

    private async Task<string> SaveOneEmployeeAsync(string? email = null)
    {
        var service = new AddStarterService(
            _repository, new EmployeeNoGenerator(), NullLogger<AddStarterService>.Instance);

        var result = await service.AddStarterAsync(new AddStarterRequest
        {
            Title = "Mr",
            Firstname = "Freddie",
            Surname = "Mercury",
            Dob = new DateOnly(1988, 9, 5),
            Gender = "Male",
            Email = email ?? "update.me@example.test",
            Address = "1 Wembley Way, London",
        });

        Assert.True(result.IsSuccess);
        return result.Employee!.EmployeeNo;
    }

    private static UpdateEmployeeRequest UpdatedRequest() => new()
    {
        Title = "Mrs",
        Firstname = "Farrokh",
        Surname = "Bulsara",
        Dob = new DateOnly(1989, 1, 1),
        Gender = "Prefer not to say",
        Email = "updated@example.test",
        Address = "9 New Road, London",
    };

    [Fact]
    public async Task UpdateEmployee_UpdatesAllEditableFields()
    {
        var employeeNo = await SaveOneEmployeeAsync();

        var result = await CreateService().UpdateEmployeeAsync(employeeNo, UpdatedRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("updated@example.test", result.Employee!.Email);
        Assert.Equal("Farrokh", result.Employee.Firstname);
        Assert.Equal("Bulsara", result.Employee.Surname);
        Assert.Equal(new DateOnly(1989, 1, 1), result.Employee.Dob);
        Assert.Equal(Gender.PreferNotToSay, result.Employee.Gender);
    }

    [Fact]
    public async Task UpdateEmployee_NeverChangesTheEmployeeNumber()
    {
        var employeeNo = await SaveOneEmployeeAsync();

        var result = await CreateService().UpdateEmployeeAsync(employeeNo, UpdatedRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(employeeNo, result.Employee!.EmployeeNo);
    }

    [Fact]
    public async Task UpdateEmployee_UnknownNumber_ReturnsNotFound()
    {
        var result = await CreateService().UpdateEmployeeAsync("EMP-000001", UpdatedRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateEmployeeError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateEmployee_WhitespaceNumber_ReturnsNotFound()
    {
        var result = await CreateService().UpdateEmployeeAsync("  ", UpdatedRequest());

        Assert.Equal(UpdateEmployeeError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateEmployee_MissingRequiredFields_ReturnsMissingRequiredFields()
    {
        var employeeNo = await SaveOneEmployeeAsync();

        var result = await CreateService().UpdateEmployeeAsync(
            employeeNo,
            UpdatedRequest() with { Surname = null });

        Assert.Equal(UpdateEmployeeError.MissingRequiredFields, result.Error);
        Assert.Contains("Surname", result.Message);
    }

    [Fact]
    public async Task UpdateEmployee_InvalidEmail_ReturnsInvalidEmail()
    {
        var employeeNo = await SaveOneEmployeeAsync();

        var result = await CreateService().UpdateEmployeeAsync(
            employeeNo,
            UpdatedRequest() with { Email = "not-an-email" });

        Assert.Equal(UpdateEmployeeError.InvalidEmail, result.Error);
    }

    [Fact]
    public async Task UpdateEmployee_InvalidGender_ReturnsInvalidGender()
    {
        var employeeNo = await SaveOneEmployeeAsync();

        var result = await CreateService().UpdateEmployeeAsync(
            employeeNo,
            UpdatedRequest() with { Gender = "Banana" });

        Assert.Equal(UpdateEmployeeError.InvalidGender, result.Error);
        Assert.Contains("Male, Female, Other, Prefer not to say", result.Message);
    }

    [Fact]
    public async Task UpdateEmployee_FutureDob_ReturnsFutureDateOfBirth()
    {
        var employeeNo = await SaveOneEmployeeAsync();

        var result = await CreateService().UpdateEmployeeAsync(
            employeeNo,
            UpdatedRequest() with { Dob = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) });

        Assert.Equal(UpdateEmployeeError.FutureDateOfBirth, result.Error);
    }

    [Fact]
    public async Task UpdateEmployee_DuplicateEmailOfOtherEmployee_ReturnsDuplicateEmail()
    {
        var firstNo = await SaveOneEmployeeAsync("first@example.test");
        await SaveOneEmployeeAsync("second@example.test");

        var result = await CreateService().UpdateEmployeeAsync(
            firstNo,
            UpdatedRequest() with { Email = "second@example.test" });

        Assert.Equal(UpdateEmployeeError.DuplicateEmail, result.Error);
    }

    [Fact]
    public async Task UpdateEmployee_SameEmailAlreadyOwnedByTarget_Succeeds()
    {
        // Re-submitting the employee's own email is an update, not a conflict.
        var employeeNo = await SaveOneEmployeeAsync("mine@example.test");

        var result = await CreateService().UpdateEmployeeAsync(
            employeeNo,
            UpdatedRequest() with { Email = "mine@example.test" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Farrokh", result.Employee!.Firstname);
    }
}
