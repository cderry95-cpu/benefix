namespace Benefits.Domain;

/// <summary>
/// Gender options collected for a new starter. Serialized to the API as
/// human-readable, spaced strings (e.g. "Prefer not to say") via a
/// System.Text.Json enum naming policy registered in the Api layer.
/// </summary>
public enum Gender
{
    Male,
    Female,
    Other,
    PreferNotToSay,
}
