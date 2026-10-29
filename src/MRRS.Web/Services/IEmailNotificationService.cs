namespace MRRS.Web.Services;

public interface IEmailNotificationService
{
    Task<EmailSendResult> SendBookingConfirmedAsync(BookingEmailModel model, CancellationToken cancellationToken);
    Task<EmailSendResult> SendBookingCancelledAsync(BookingEmailModel model, CancellationToken cancellationToken);
}

public sealed record EmailSendResult(bool Sent, string? Warning);

public sealed record BookingEmailModel(
    int BookingId,
    string Title,
    string RoomName,
    string Location,
    DateTime Start,
    DateTime End,
    string OrganizerName,
    string OrganizerEmail,
    string? Notes,
    IReadOnlyList<string> AttendeeEmails);
