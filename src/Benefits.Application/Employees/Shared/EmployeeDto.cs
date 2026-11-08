namespace Benefits.Application.Employees;

using Benefits.Domain;

/// <summary>
/// A saved employee as returned by the API.
/// </summary>
public sealed record EmployeeDto
{
    public required int EmployeeNo { get; init; }
    public required string Email { get; init; }
    public string? Title { get; init; }
    public string? Firstname { get; init; }
    public string? Surname { get; init; }
    public DateOnly? Dob { get; init; }
    public Gender? Gender { get; init; }
    public string? Address { get; init; }

    /// <summary>Maps the domain entity to its API representation; every
    /// service that returns an employee uses this one mapping.</summary>
    public static EmployeeDto FromDomain(Domain.Employee employee) => new()
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
