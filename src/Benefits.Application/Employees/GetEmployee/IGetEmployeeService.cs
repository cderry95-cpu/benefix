namespace Benefits.Application.Employees;

/// <summary>Use case: look up a single employee by their employee number.</summary>
public interface IGetEmployeeService
{
    Task<GetEmployeeResult> GetEmployeeAsync(string employeeNo, CancellationToken cancellationToken = default);
}
