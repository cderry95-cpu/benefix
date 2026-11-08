namespace Benefits.Application.Employees;

/// <summary>Outcome of a list-employees use case.</summary>
public sealed record ListEmployeesResult(EmployeePage? EmployeePage, ListEmployeesError Error, string? Message = null)
{
    public static ListEmployeesResult Success(EmployeePage page) => new(page, ListEmployeesError.None);

    public static ListEmployeesResult Failure(ListEmployeesError error, string? message = null) =>
        new(null, error, message);

    public bool IsSuccess => Error is ListEmployeesError.None && EmployeePage is not null;
}
