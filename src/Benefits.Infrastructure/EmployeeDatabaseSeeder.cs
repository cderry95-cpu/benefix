using Benefits.Application.Employees;
using Benefits.Domain;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Benefits.Infrastructure;

/// <summary>
/// Seeds a fixed set of development employees on startup so the API has data
/// to list and page through in local development. No-op in production, and
/// idempotent: it only inserts when the table is empty, so restarting or
/// using the API never duplicates or overwrites real data.
/// </summary>
public sealed class EmployeeDatabaseSeeder(
    EmployeeDatabase database,
    IEmployeeNoGenerator employeeNoGenerator,
    IHostEnvironment environment,
    ILogger<EmployeeDatabaseSeeder> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => SeedAsync(cancellationToken);

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogInformation(
                "Employee seeding skipped: production never seeds (environment is '{Environment}').",
                environment.EnvironmentName);
            return;
        }

        var existing = await database.Connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM Employees",
            cancellationToken: cancellationToken));

        if (existing is not 0)
        {
            logger.LogInformation("Employee seeding skipped: {Count} employees already exist.", existing);
            return;
        }

        foreach (var seed in SeedEmployees)
        {
            var employee = new Employee
            {
                EmployeeNo = employeeNoGenerator.Generate(),
                Email = $"{seed.First}.{seed.Last}@example.test".ToLowerInvariant(),
                Title = seed.Title,
                Firstname = seed.First,
                Surname = seed.Last,
                Dob = new DateOnly(seed.Year, seed.Month, seed.Day),
                Gender = seed.Gender,
                Address = seed.Address,
            };

            await database.Connection.ExecuteAsync(new CommandDefinition(
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
        }

        logger.LogInformation("Seeded {Count} development employees.", SeedEmployees.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Exactly 25 realistic records so pagination has more than one page of
    /// data by default (page size is 10). Emails are unique by construction.
    /// </summary>
    private static readonly IReadOnlyList<(string? Title, string First, string Last, Gender Gender, int Year, int Month, int Day, string? Address)>
        SeedEmployees =
        [
            ("Mr", "Oliver", "Bennett", Gender.Male, 1990, 3, 24, "1 Sentinel Way, London"),
            ("Mrs", "Amelia", "Clarke", Gender.Female, 1984, 7, 9, "2 Kings Road, Manchester"),
            ("Ms", "Isla", "Whitmore", Gender.Female, 1994, 1, 17, null),
            ("Dr", "George", "Ferris", Gender.Male, 1979, 11, 2, "14 Park Lane, Leeds"),
            ("Mr", "Raj", "Patel", Gender.Male, 1992, 5, 30, "7 Ivy Street, Birmingham"),
            (null, "Freya", "Dawson", Gender.Female, 1997, 2, 12, null),
            ("Mr", "Thomas", "Whitaker", Gender.Male, 1983, 9, 21, "31 Mill Lane, Sheffield"),
            ("Mrs", "Sofia", "Lindgren", Gender.Female, 1988, 12, 4, "8 Harbour Close, Bristol"),
            ("Mx", "Jamie", "Sinclair", Gender.PreferNotToSay, 1991, 6, 15, null),
            ("Mr", "Nathan", "Cole", Gender.Male, 1996, 8, 28, "22 Elm Court, Nottingham"),
            ("Ms", "Priya", "Raman", Gender.Female, 1986, 4, 7, "5 Beacon Hill, Cardiff"),
            (null, "Lucas", "Meyer", Gender.Male, 1993, 10, 19, null),
            ("Mr", "Ibrahim", "Diallo", Gender.Male, 1989, 1, 26, "47 Rosewood Drive, Glasgow"),
            ("Mrs", "Hannah", "Doyle", Gender.Female, 1995, 3, 8, "19 Stonebridge, Edinburgh"),
            ("Ms", "Beatriz", "Sousa", Gender.Female, 1990, 9, 14, null),
            ("Dr", "Aaron", "Christie", Gender.Male, 1975, 7, 3, "3 Orchard Rise, Newcastle"),
            (null, "Ivo", "Petrov", Gender.Male, 1998, 5, 22, null),
            ("Mrs", "Ellen", "O'Donnell", Gender.Female, 1981, 11, 30, "60 Willow Bend, Dublin"),
            ("Ms", "Zainab", "Hussain", Gender.Female, 1992, 2, 6, "12 Tanner Street, Liverpool"),
            ("Mr", "Marcus", "Bell", Gender.Male, 1987, 6, 11, "9 Foxglove Lane, Oxford"),
            (null, "Emily", "Ward", Gender.Female, 1999, 12, 25, null),
            ("Mr", "Viktor", "Hansen", Gender.Male, 1985, 8, 18, "28 Millbrook, Cambridge"),
            ("Ms", "Naomi", "Ellery", Gender.Female, 1993, 4, 23, null),
            ("Mr", "Sean", "Miller", Gender.Male, 1994, 10, 1, "16 Copperfield, Warwick"),
            ("Ms", "Amara", "Osei", Gender.Other, 1991, 7, 29, "40 Sangria Row, London"),
        ];
}
