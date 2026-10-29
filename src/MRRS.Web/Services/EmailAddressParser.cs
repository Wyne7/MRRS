using System.Text.RegularExpressions;

namespace MRRS.Web.Services;

public static partial class EmailAddressParser
{
    public static IReadOnlyList<string> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var parts = raw.Split([',', ';', '\n', '\r', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var emails = new List<string>();
        foreach (var part in parts)
        {
            if (!EmailPattern().IsMatch(part))
            {
                throw new InvalidBookingException($"'{part}' is not a valid email address.");
            }

            if (!emails.Contains(part, StringComparer.OrdinalIgnoreCase))
            {
                emails.Add(part);
            }
        }

        return emails;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
