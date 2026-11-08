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
}
