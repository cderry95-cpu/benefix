using Microsoft.Extensions.Logging;

namespace Benefits.Application.Employees;

/// <summary>
/// Use case: list employees one page at a time, ordered alphabetically by
/// surname then firstname (email breaks ties so pagination is deterministic).
/// Reading a page past the last one returns an empty page with correct
/// metadata rather than an error.
/// </summary>
public interface IListEmployeesService
{
    Task<ListEmployeesResult> ListEmployeesAsync(ListEmployeesRequest request, CancellationToken cancellationToken = default);
}

public sealed class ListEmployeesService(IEmployeeRepository repository, ILogger<ListEmployeesService> logger)
    : IListEmployeesService
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public async Task<ListEmployeesResult> ListEmployeesAsync(ListEmployeesRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page ?? DefaultPage;
        var pageSize = request.PageSize ?? DefaultPageSize;

        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            var message =
                $"page must be at least 1, and pageSize must be between 1 and {MaxPageSize} (got page={request.Page ?? DefaultPage}, pageSize={request.PageSize ?? DefaultPageSize}).";
            logger.LogWarning("Employees list rejected: {Message}", message);
            return ListEmployeesResult.Failure(ListEmployeesError.InvalidPagination, message);
        }

        var (employees, totalCount) = await repository.ListAsync(page, pageSize, cancellationToken);

        var items = employees.Select(Map).ToList();

        logger.LogInformation(
            "Listed {Count} of {TotalCount} employees (page={Page}, pageSize={PageSize}).",
            items.Count, totalCount, page, pageSize);

        return ListEmployeesResult.Success(new EmployeePage(items, page, pageSize, totalCount));
    }

    private static EmployeeDto Map(Domain.Employee employee) => new()
    {
        EmployeeNo = employee.EmployeeNo,
        Email = employee.Email,
        Title = employee.Title,
        Firstname = employee.Firstname,
        Surname = employee.Surname,
        Dob = employee.Dob,
        Gender = employee.Gender,
        Address = employee.Address,
    };
}
