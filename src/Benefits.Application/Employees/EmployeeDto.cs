namespace Benefits.Application.Employees;

using Benefits.Domain;

/// <summary>
/// A saved employee as returned by the API.
/// </summary>
public sealed record EmployeeDto
{
    public required string EmployeeNo { get; init; }
    public required string Email { get; init; }
    public string? Title { get; init; }
    public string? Firstname { get; init; }
    public string? Surname { get; init; }
    public DateOnly? Dob { get; init; }
    public Gender? Gender { get; init; }
    public string? Address { get; init; }
}
