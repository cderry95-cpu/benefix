namespace Benefits.Application.Employees;

/// <summary>Use case: update an existing employee's editable fields.</summary>
public interface IUpdateEmployeeService
{
    Task<UpdateEmployeeResult> UpdateEmployeeAsync(
        int employeeNo,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);
}
