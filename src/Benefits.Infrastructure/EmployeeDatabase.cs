using Microsoft.Data.Sqlite;

namespace Benefits.Infrastructure;

/// <summary>
/// Owns the single open in-memory SQLite connection. SQLite keeps an in-memory
/// database alive only while at least one connection is open, so this instance
/// is registered as a singleton and holds the connection for the process lifetime.
/// </summary>
public sealed class EmployeeDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public EmployeeDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        CreateSchema();
    }

    internal SqliteConnection Connection => _connection;

    private void CreateSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Employees (
                EmployeeNo INTEGER NOT NULL PRIMARY KEY,
                Title      TEXT,
                Firstname  TEXT,
                Surname    TEXT,
                Dob        TEXT,
                Gender     INTEGER,
                Email      TEXT NOT NULL,
                Address    TEXT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_Employees_Email
                ON Employees (Email);
            """;
        command.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}
