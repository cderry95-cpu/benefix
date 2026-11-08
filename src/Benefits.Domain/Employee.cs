namespace Benefits.Domain;

/// <summary>
/// A stored employee. <see cref="EmployeeNo"/> is a generated GUID assigned by
/// the Application layer; <see cref="Email"/> is unique across all employees.
/// </summary>
public sealed record Employee
{
    public required string EmployeeNo { get; init; }
    public required string Email { get; init; }
    public required string Firstname { get; init; }
    public required string Surname { get; init; }

    public string? Title { get; init; }
    public DateOnly? Dob { get; init; }
    public Gender? Gender { get; init; }
    public string? Address { get; init; }
}
