namespace Benefits.Application.Employees;

/// <summary>Use case: permanently delete an employee by their employee number.</summary>
public interface IDeleteEmployeeService
{
    Task<DeleteEmployeeResult> DeleteEmployeeAsync(int employeeNo, CancellationToken cancellationToken = default);
}
