using Benefits.Application.Employees;
using Benefits.Domain;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Benefits.Infrastructure;

public sealed class EmployeeRepository(EmployeeDatabase database) : IEmployeeRepository
{
    public async Task<bool> AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await database.Connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO Employees (EmployeeNo, Title, Firstname, Surname, Dob, Gender, Email, Address)
                VALUES (@EmployeeNo, @Title, @Firstname, @Surname, @Dob, @Gender, @Email, @Address)
                """,
                new
                {
                    employee.EmployeeNo,
                    employee.Title,
                    employee.Firstname,
                    employee.Surname,
                    Dob = employee.Dob?.ToString("O"),
                    Gender = (int?)employee.Gender,
                    employee.Email,
                    employee.Address,
                },
                cancellationToken: cancellationToken));

            return rows is 1;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 19) // SQLITE_CONSTRAINT
        {
            return false;
        }
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var existing = await database.Connection.ExecuteScalarAsync<object?>(new CommandDefinition(
            "SELECT 1 FROM Employees WHERE Email = @Email LIMIT 1",
            new { Email = email },
            cancellationToken: cancellationToken));

        return existing is not null;
    }
}
