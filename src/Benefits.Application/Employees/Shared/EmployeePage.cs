namespace Benefits.Application.Employees;

/// <summary>
/// One page of the employee collection. <see cref="TotalCount"/> is the number
/// of employees across all pages, so clients can compute the page count.
/// </summary>
public sealed record EmployeePage(IReadOnlyList<EmployeeDto> Items, int Page, int PageSize, int TotalCount);
