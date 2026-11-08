using Benefits.Domain;

namespace Benefits.Application.Employees;

/// <summary>
/// Request to update an existing employee's details. The employee number is
/// the identity of the resource and lives in the route only — it cannot be
/// changed through this endpoint. Required and format rules match add: email,
/// firstname and surname are required; dob must not be in the future; gender
/// accepts the same wire strings ("Male", "Prefer not to say", ...).
/// </summary>
public sealed record UpdateEmployeeRequest
{
    public string? Title { get; init; }
    public string? Firstname { get; init; }
    public string? Surname { get; init; }
    public DateOnly? Dob { get; init; }
    public string? Gender { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
}
