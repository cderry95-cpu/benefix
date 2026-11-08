namespace Benefits.Application.Employees;

/// <summary>Outcome of an update-employee use case.</summary>
public sealed record UpdateEmployeeResult(EmployeeDto? Employee, UpdateEmployeeError Error, string? Message = null)
{
    public static UpdateEmployeeResult Success(EmployeeDto employee) => new(employee, UpdateEmployeeError.None);

    public static UpdateEmployeeResult Failure(UpdateEmployeeError error, string? message = null) =>
        new(null, error, message);

    public bool IsSuccess => Error is UpdateEmployeeError.None && Employee is not null;
}
