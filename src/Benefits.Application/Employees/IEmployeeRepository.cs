using Benefits.Domain;

namespace Benefits.Application.Employees;

/// <summary>
/// Persistence contract for employees. Uniqueness of <see cref="Employee.Email"/>
/// is enforced both by explicit existence checks and by a unique index in the
/// database, so <see cref="AddAsync"/> returns false when the database rejects
/// the insert as a duplicate.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>Inserts the employee; returns false if the email is already taken.</summary>
    Task<bool> AddAsync(Employee employee, CancellationToken cancellationToken = default);

    /// <summary>Returns true if an employee with this email already exists.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if an employee with this employee number already exists.
    /// Backs the generator's collision-retry loop; the database primary key
    /// on EmployeeNo remains the authoritative guard.
    /// </summary>
    Task<bool> EmployeeNoExistsAsync(string employeeNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of employees, alphabetically ordered by surname,
    /// firstname, then email, plus the total number of employees.
    /// </summary>
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the employee with this employee number, or null when absent.</summary>
    Task<Employee?> GetByEmployeeNoAsync(string employeeNo, CancellationToken cancellationToken = default);
}
