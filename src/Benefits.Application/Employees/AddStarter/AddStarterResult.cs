namespace Benefits.Application.Employees;

/// <summary>Outcome of an add-starter use case.</summary>
public sealed record AddStarterResult(EmployeeDto? Employee, AddStarterError Error, string? Message = null)
{
    public static AddStarterResult Success(EmployeeDto employee) => new(employee, AddStarterError.None);

    public static AddStarterResult Failure(AddStarterError error, string? message = null) => new(null, error, message);

    public bool IsSuccess => Error is AddStarterError.None && Employee is not null;
}
