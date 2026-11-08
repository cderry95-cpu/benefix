namespace Benefits.Application.Employees;

/// <summary>
/// Query to list employees with offset pagination. Both values are optional;
/// <see cref="ListEmployeesService.DefaultPage"/> and
/// <see cref="ListEmployeesService.DefaultPageSize"/> apply when omitted.
/// </summary>
public sealed record ListEmployeesRequest(int? Page, int? PageSize);
