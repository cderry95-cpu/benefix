namespace Benefits.Application.Employees;

/// <summary>Use case: list employees with pagination.</summary>
public interface IListEmployeesService
{
    Task<ListEmployeesResult> ListEmployeesAsync(ListEmployeesRequest request, CancellationToken cancellationToken = default);
}
