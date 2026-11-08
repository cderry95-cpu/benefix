using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Use case: look up a single employee by their employee number.
/// </summary>
public interface IGetEmployeeService
{
    Task<GetEmployeeResult> GetEmployeeAsync(string employeeNo, CancellationToken cancellationToken = default);
}

public sealed class GetEmployeeService(IEmployeeRepository repository, ILogger<GetEmployeeService> logger)
    : IGetEmployeeService
{
    public async Task<GetEmployeeResult> GetEmployeeAsync(string employeeNo, CancellationToken cancellationToken = default)
    {
        var normalised = employeeNo.Trim();

        if (string.IsNullOrWhiteSpace(normalised))
        {
            logger.LogWarning("Employee lookup failed: employee number is empty.");
            return GetEmployeeResult.NotFound();
        }

        var employee = await repository.GetByEmployeeNoAsync(normalised, cancellationToken);

        if (employee is null)
        {
            logger.LogWarning("Employee lookup failed: no employee with employee number '{EmployeeNo}' exists.", normalised);
            return GetEmployeeResult.NotFound();
        }

        logger.LogInformation(
            "Employee found. EmployeeNo={EmployeeNo}, Email={Email}, Name={Firstname} {Surname}.",
            employee.EmployeeNo, employee.Email, employee.Firstname, employee.Surname);

        return GetEmployeeResult.Success(ToDto(employee));
    }

    private static EmployeeDto ToDto(Domain.Employee employee) => new()
    {
        EmployeeNo = employee.EmployeeNo,
        Email = employee.Email,
        Title = employee.Title,
        Firstname = employee.Firstname,
        Surname = employee.Surname,
        Dob = employee.Dob,
        Gender = employee.Gender,
        Address = employee.Address,
    };
}
