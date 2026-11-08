namespace Benefits.Application.Employees;

/// <summary>Outcome of a delete-employee use case.</summary>
public sealed record DeleteEmployeeResult(DeleteEmployeeError Error)
{
    public static DeleteEmployeeResult Deleted() => new(DeleteEmployeeError.None);

    public static DeleteEmployeeResult NotFound() => new(DeleteEmployeeError.NotFound);

    public bool IsSuccess => Error is DeleteEmployeeError.None;
}
