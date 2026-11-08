using Benefits.Domain;

namespace Benefits.Application.Employees;

/// <summary>
/// Parses the gender values clients send on the wire. Accepts the domain enum
/// member names (case-insensitive) and their natural spaced forms, e.g.
/// "Male" or "prefer not to say". Invalid values are rejected with a 400
/// that lists <see cref="AllowedValues"/> so clients can self-correct.
/// </summary>
public static class GenderParser
{
    public static readonly IReadOnlyList<string> AllowedValues =
    [
        nameof(Gender.Male),
        nameof(Gender.Female),
        nameof(Gender.Other),
        "Prefer not to say",
    ];

    private static readonly Dictionary<string, Gender> Lookup = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(Gender.Male)] = Gender.Male,
        [nameof(Gender.Female)] = Gender.Female,
        [nameof(Gender.Other)] = Gender.Other,
        [nameof(Gender.PreferNotToSay)] = Gender.PreferNotToSay,
        ["Prefer not to say"] = Gender.PreferNotToSay,
    };

    public static bool TryParse(string? value, out Gender gender)
    {
        gender = default;
        return Lookup.TryGetValue(value!.Trim(), out gender);
    }
}
