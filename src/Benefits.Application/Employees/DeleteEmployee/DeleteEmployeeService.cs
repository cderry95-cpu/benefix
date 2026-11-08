using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Unknown numbers return NotFound rather than pretending to succeed:
/// clients need to distinguish "removed by this call" from "nothing to remove".
/// </summary>
public sealed class DeleteEmployeeService(IEmployeeRepository repository, ILogger<DeleteEmployeeService> logger)
    : IDeleteEmployeeService
{
    public async Task<DeleteEmployeeResult> DeleteEmployeeAsync(int employeeNo, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteByEmployeeNoAsync(employeeNo, cancellationToken))
        {
            logger.LogWarning("Employee delete failed: no employee with employee number {EmployeeNo} exists.", employeeNo);
            return DeleteEmployeeResult.NotFound();
        }

        logger.LogInformation("Employee deleted. EmployeeNo={EmployeeNo}.", employeeNo);

        return DeleteEmployeeResult.Deleted();
    }
}
