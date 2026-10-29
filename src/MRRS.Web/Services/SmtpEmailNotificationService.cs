using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MRRS.Web.Services;

public class SmtpEmailNotificationService : IEmailNotificationService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailNotificationService> _logger;

    public SmtpEmailNotificationService(IOptions<SmtpOptions> options, ILogger<SmtpEmailNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<EmailSendResult> SendBookingConfirmedAsync(BookingEmailModel model, CancellationToken cancellationToken) =>
        SendAsync(model, cancel: false, cancellationToken);

    public Task<EmailSendResult> SendBookingCancelledAsync(BookingEmailModel model, CancellationToken cancellationToken) =>
        SendAsync(model, cancel: true, cancellationToken);

    private async Task<EmailSendResult> SendAsync(BookingEmailModel model, bool cancel, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogInformation(
                "SMTP host is empty. Skipping {Kind} email for booking {BookingId}.",
                cancel ? "cancellation" : "invitation",
                model.BookingId);
            return new EmailSendResult(false, "SMTP is not configured (Smtp:Host is empty), so no invitation was sent.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            return new EmailSendResult(false, "SMTP from address is empty, so no invitation was sent.");
        }

        var recipients = new List<string>();
        if (!string.IsNullOrWhiteSpace(model.OrganizerEmail))
        {
            recipients.Add(model.OrganizerEmail);
        }

        recipients.AddRange(model.AttendeeEmails);
        recipients = recipients
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            return new EmailSendResult(false, "No recipient addresses were available, so no invitation was sent.");
        }

        try
        {
            var message = BuildMessage(model, recipients, cancel);
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, MapSecurity(_options.Security), cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return new EmailSendResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for booking {BookingId}.", model.BookingId);
            return new EmailSendResult(false, "The reservation was saved, but the SMTP server rejected the invitation.");
        }
    }

    private MimeMessage BuildMessage(BookingEmailModel model, IReadOnlyList<string> recipients, bool cancel)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        message.Subject = cancel
            ? $"Cancelled: {model.RoomName} — {model.Title}"
            : $"Room reserved: {model.RoomName} — {model.Title}";

        var when = $"{model.Start:dddd, MMMM d, yyyy} · {model.Start:h:mm tt} – {model.End:h:mm tt}";
        var headline = cancel ? "Reservation cancelled" : "Room reserved";
        var plainNotes = string.IsNullOrWhiteSpace(model.Notes) ? string.Empty : $"\nNotes: {model.Notes}";
        var plain = $"""
            {headline}

            {model.Title}
            {model.Location}
            {when}
            Organizer: {model.OrganizerName}
            {plainNotes}

            A calendar invitation is attached. Times are the organization's local time.
            """;

        var html = $"""
            <div style="font-family: Georgia, 'Iowan Old Style', serif; color: #1c1915; line-height: 1.45;">
              <p style="letter-spacing: 0.14em; text-transform: uppercase; font-size: 12px; margin: 0 0 12px;">MRRS</p>
              <h1 style="font-size: 28px; font-weight: 560; margin: 0 0 16px;">{headline}</h1>
              <p style="font-size: 18px; margin: 0 0 8px;"><strong>{WebUtility.HtmlEncode(model.Title)}</strong></p>
              <p style="margin: 0 0 8px;">{WebUtility.HtmlEncode(model.Location)}</p>
              <p style="margin: 0 0 8px;">{WebUtility.HtmlEncode(when)}</p>
              <p style="margin: 0 0 8px;">Organizer: {WebUtility.HtmlEncode(model.OrganizerName)}</p>
              {(string.IsNullOrWhiteSpace(model.Notes) ? "" : $"<p style=\"margin: 12px 0 0;\">{WebUtility.HtmlEncode(model.Notes)}</p>")}
            </div>
            """;

        var calendar = new TextPart("calendar")
        {
            Text = IcsInviteBuilder.Build(model, cancel),
            ContentDisposition = new ContentDisposition(ContentDisposition.Inline)
        };
        calendar.ContentType.Parameters["method"] = cancel ? "CANCEL" : "REQUEST";
        calendar.ContentType.Charset = "utf-8";
        calendar.FileName = "invite.ics";

        message.Body = new Multipart("alternative")
        {
            new TextPart("plain") { Text = plain },
            new TextPart("html") { Text = html },
            calendar
        };

        return message;
    }

    private static SecureSocketOptions MapSecurity(SmtpSecurity security) => security switch
    {
        SmtpSecurity.None => SecureSocketOptions.None,
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.StartTls
    };
}
