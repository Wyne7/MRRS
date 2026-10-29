using System.Text;

namespace MRRS.Web.Services;

public static class IcsInviteBuilder
{
    public static string Build(BookingEmailModel model, bool cancel)
    {
        var method = cancel ? "CANCEL" : "REQUEST";
        var status = cancel ? "CANCELLED" : "CONFIRMED";
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
        var start = model.Start.ToString("yyyyMMdd'T'HHmmss");
        var end = model.End.ToString("yyyyMMdd'T'HHmmss");
        var description = $"{model.Title} in {model.RoomName}. Organizer: {model.OrganizerName}.";
        if (!string.IsNullOrWhiteSpace(model.Notes))
        {
            description += " " + model.Notes;
        }

        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "PRODID:-//MRRS//Meeting Room Reservation//EN",
            "VERSION:2.0",
            "CALSCALE:GREGORIAN",
            $"METHOD:{method}",
            "BEGIN:VEVENT",
            $"UID:booking-{model.BookingId}@mrrs.local",
            $"DTSTAMP:{stamp}",
            $"DTSTART:{start}",
            $"DTEND:{end}",
            $"SUMMARY:{Escape(model.Title)}",
            $"LOCATION:{Escape(model.Location)}",
            $"DESCRIPTION:{Escape(description)}",
            $"STATUS:{status}",
            $"SEQUENCE:{(cancel ? 1 : 0)}",
            $"ORGANIZER;CN={Escape(model.OrganizerName)}:mailto:{model.OrganizerEmail}"
        };

        foreach (var email in model.AttendeeEmails)
        {
            lines.Add($"ATTENDEE;ROLE=REQ-PARTICIPANT;RSVP=FALSE:mailto:{email}");
        }

        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");

        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            AppendFolded(builder, line);
        }

        return builder.ToString();
    }

    private static void AppendFolded(StringBuilder builder, string line)
    {
        const int limit = 73;
        if (line.Length <= limit)
        {
            builder.Append(line).Append("\r\n");
            return;
        }

        builder.Append(line.AsSpan(0, limit)).Append("\r\n");
        var index = limit;
        while (index < line.Length)
        {
            var take = Math.Min(limit - 1, line.Length - index);
            builder.Append(' ').Append(line.AsSpan(index, take)).Append("\r\n");
            index += take;
        }
    }

    private static string Escape(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
