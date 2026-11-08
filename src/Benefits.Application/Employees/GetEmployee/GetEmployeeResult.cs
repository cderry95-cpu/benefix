namespace Benefits.Application.Employees;

/// <summary>Outcome of a get-employee use case.</summary>
public sealed record GetEmployeeResult(EmployeeDto? Employee, GetEmployeeError Error)
{
    public static GetEmployeeResult Success(EmployeeDto employee) => new(employee, GetEmployeeError.None);

    public static GetEmployeeResult NotFound() => new(null, GetEmployeeError.NotFound);

    public bool IsSuccess => Error is GetEmployeeError.None && Employee is not null;
}
