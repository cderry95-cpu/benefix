using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Looks up a single employee by their employee number.
/// </summary>
public sealed class GetEmployeeService(IEmployeeRepository repository, ILogger<GetEmployeeService> logger)
    : IGetEmployeeService
{
    public async Task<GetEmployeeResult> GetEmployeeAsync(int employeeNo, CancellationToken cancellationToken = default)
    {
        var employee = await repository.GetByEmployeeNoAsync(employeeNo, cancellationToken);

        if (employee is null)
        {
            logger.LogWarning("Employee lookup failed: no employee with employee number {EmployeeNo} exists.", employeeNo);
            return GetEmployeeResult.NotFound();
        }

        logger.LogInformation(
            "Employee found. EmployeeNo={EmployeeNo}, Email={Email}, Name={Firstname} {Surname}.",
            employee.EmployeeNo, employee.Email, employee.Firstname, employee.Surname);

        return GetEmployeeResult.Success(EmployeeDto.FromDomain(employee));
    }
}
