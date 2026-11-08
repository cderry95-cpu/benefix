using Benefits.Application.Employees;
using Benefits.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace Benefits.Api.Tests;

public class AddStarterServiceTests
{
    private readonly FakeEmployeeRepository _repository = new();
    private readonly IEmployeeNoGenerator _employeeNoGenerator = new EmployeeNoGenerator();

    private AddStarterService CreateService() =>
        new(_repository, _employeeNoGenerator, NullLogger<AddStarterService>.Instance);

    private static AddStarterRequest ValidRequest() => new()
    {
        Title = "Mr",
        Firstname = "Jimi",
        Surname = "Hendrix",
        Dob = new DateOnly(1988, 4, 13),
        Gender = "Male",
        Email = "jimi.hendrix@example.com",
        Address = "12 Electric Ladyland, London",
    };

    [Fact]
    public async Task AddStarter_SavesAndReturnsEmployee()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Employee);
        Assert.Equal("jimi.hendrix@example.com", result.Employee.Email);
        Assert.Equal("Jimi", result.Employee.Firstname);
        Assert.Equal(new DateOnly(1988, 4, 13), result.Employee.Dob);
        Assert.Equal(Gender.Male, result.Employee.Gender);
        Assert.Single(_repository.Saved);
    }

    [Fact]
    public async Task AddStarter_GeneratesEmployeeNoInEmpFormat()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest());

        Assert.Matches(@"^EMP-\d{6}$", result.Employee!.EmployeeNo);
    }

    [Fact]
    public async Task AddStarter_GeneratesUniqueEmployeeNoForEachEmployee()
    {
        var service = CreateService();

        var first = await service.AddStarterAsync(ValidRequest());
        var second = await service.AddStarterAsync(ValidRequest() with
        {
            Email = "tim@example.com",
        });

        Assert.NotEqual(first.Employee!.EmployeeNo, second.Employee!.EmployeeNo);
        Assert.Equal(2, _repository.Saved.Count);
    }

    [Fact]
    public async Task AddStarter_DuplicateEmail_ReturnsDuplicateEmailError()
    {
        var service = CreateService();
        await service.AddStarterAsync(ValidRequest());

        var result = await service.AddStarterAsync(ValidRequest() with
        {
            Firstname = "Another",
            Email = "jimi.hendrix@example.com",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AddStarterError.DuplicateEmail, result.Error);
        Assert.Null(result.Employee);
        Assert.Single(_repository.Saved);
    }

    [Fact]
    public async Task AddStarter_InvalidEmail_ReturnsInvalidEmailError()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest() with { Email = "not-an-email" });

        Assert.Equal(AddStarterError.InvalidEmail, result.Error);
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task AddStarter_MissingEmail_ReturnsMissingRequiredFieldsError()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest() with { Email = null });

        Assert.Equal(AddStarterError.MissingRequiredFields, result.Error);
        Assert.Contains("Email", result.Message);
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task AddStarter_MissingFirstname_ReturnsMissingRequiredFieldsError()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest() with { Firstname = null });

        Assert.Equal(AddStarterError.MissingRequiredFields, result.Error);
        Assert.Contains("Firstname", result.Message);
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task AddStarter_MissingSurname_ReturnsMissingRequiredFieldsError()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest() with { Surname = "" });

        Assert.Equal(AddStarterError.MissingRequiredFields, result.Error);
        Assert.Contains("Surname", result.Message);
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task AddStarter_FutureDob_ReturnsFutureDateOfBirthError()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest() with
        {
            Dob = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        });

        Assert.Equal(AddStarterError.FutureDateOfBirth, result.Error);
        Assert.Contains("future", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task AddStarter_GenderAsSpacedString_Parses()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest() with { Gender = "Prefer not to say" });

        Assert.True(result.IsSuccess);
        Assert.Equal(Gender.PreferNotToSay, result.Employee!.Gender);
    }

    [Fact]
    public async Task AddStarter_GenderCaseInsensitive_Parses()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest() with { Gender = "FEMALE" });

        Assert.True(result.IsSuccess);
        Assert.Equal(Gender.Female, result.Employee!.Gender);
    }

    [Fact]
    public async Task AddStarter_InvalidGender_ReturnsInvalidGenderErrorWithAllowedValues()
    {
        var service = CreateService();

        var result = await service.AddStarterAsync(ValidRequest() with { Gender = "Banana" });

        Assert.Equal(AddStarterError.InvalidGender, result.Error);
        Assert.Contains("Banana", result.Message);
        Assert.Contains("Male", result.Message);
        Assert.Contains("Prefer not to say", result.Message);
        Assert.Empty(_repository.Saved);
    }
}

/// <summary>
/// In-memory fake used by unit tests: behaves like the unique-index database
/// without requiring SQLite.
/// </summary>
public sealed class FakeEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Saved { get; } = [];

    private readonly HashSet<string> _emails = new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _employeeNos = new(StringComparer.Ordinal);

    public Task<bool> AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        if (!_employeeNos.Add(employee.EmployeeNo))
        {
            return Task.FromResult(false);
        }

        if (!_emails.Add(employee.Email))
        {
            _employeeNos.Remove(employee.EmployeeNo);
            return Task.FromResult(false);
        }

        Saved.Add(employee);
        return Task.FromResult(true);
    }

    public Task<bool> EmployeeNoExistsAsync(string employeeNo, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_employeeNos.Contains(employeeNo));
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_emails.Contains(email));
    }

    public Task<(IReadOnlyList<Employee> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = Saved
            .OrderBy(e => e.Surname, StringComparer.Ordinal)
            .ThenBy(e => e.Firstname, StringComparer.Ordinal)
            .ThenBy(e => e.Email, StringComparer.Ordinal)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Employee>)items, Saved.Count));
    }
}
