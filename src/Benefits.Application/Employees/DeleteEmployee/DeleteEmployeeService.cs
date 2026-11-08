using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Unknown numbers return NotFound rather than pretending to succeed:
/// clients need to distinguish "removed by this call" from "nothing to remove".
/// </summary>
public sealed class DeleteEmployeeService(IEmployeeRepository repository, ILogger<DeleteEmployeeService> logger)
    : IDeleteEmployeeService
{
    public async Task<DeleteEmployeeResult> DeleteEmployeeAsync(string employeeNo, CancellationToken cancellationToken = default)
    {
        var normalised = employeeNo.Trim();

        if (string.IsNullOrWhiteSpace(normalised))
        {
            logger.LogWarning("Employee delete failed: employee number is empty.");
            return DeleteEmployeeResult.NotFound();
        }

        if (!await repository.DeleteByEmployeeNoAsync(normalised, cancellationToken))
        {
            logger.LogWarning("Employee delete failed: no employee with employee number '{EmployeeNo}' exists.", normalised);
            return DeleteEmployeeResult.NotFound();
        }

        logger.LogInformation("Employee deleted. EmployeeNo={EmployeeNo}.", normalised);

        return DeleteEmployeeResult.Deleted();
    }
}
