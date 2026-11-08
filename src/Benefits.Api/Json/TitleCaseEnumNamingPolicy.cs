using System.Text;
using System.Text.Json;

namespace Benefits.Api.Json;

/// <summary>
/// Converts enum member names to spaced, title-case strings so that enums read
/// naturally over the wire, e.g. "PreferNotToSay" becomes "Prefer not to say".
/// The same conversion is applied when deserializing, which keeps the accepted
/// request values aligned with the produced response values.
/// </summary>
public sealed class TitleCaseEnumNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        var converted = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (i > 0 && char.IsUpper(current))
            {
                converted.Append(' ');
                converted.Append(char.ToLowerInvariant(current));
            }
            else
            {
                converted.Append(current);
            }
        }

        return converted.ToString();
    }
}
