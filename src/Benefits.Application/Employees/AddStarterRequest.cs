namespace Benefits.Application.Employees;

using Benefits.Domain;

/// <summary>
/// Request to add a new starter. Only <see cref="Email"/>, <see cref="Firstname"/>
/// and <see cref="Surname"/> are required; all other fields are optional.
/// <see cref="Gender"/> is the wire string form of the domain <c>Gender</c>
/// enum, e.g. "Male" or "Prefer not to say"; invalid values are rejected with
/// a 400 that lists the allowed values.
/// </summary>
public sealed record AddStarterRequest
{
    public string? Title { get; init; }
    public string? Firstname { get; init; }
    public string? Surname { get; init; }
    public DateOnly? Dob { get; init; }
    public string? Gender { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
}
