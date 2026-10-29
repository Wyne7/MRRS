using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MRRS.Web.Data;
using MRRS.Web.Models;

namespace MRRS.Web.Services;

public sealed record CreateBookingCommand(
    int RoomId,
    string OrganizerId,
    string Title,
    string? Notes,
    DateTime Start,
    DateTime End,
    string? AttendeeEmails);

public sealed record BookingMutationResult(Booking Booking, bool EmailSent, string? EmailWarning);

public class BookingService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailNotificationService _email;
    private readonly ILogger<BookingService> _logger;

    public BookingService(ApplicationDbContext db, IEmailNotificationService email, ILogger<BookingService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Booking>> SearchAsync(
        DateTime from,
        DateTime to,
        int? roomId,
        string? organizerId,
        bool includeCancelled,
        CancellationToken cancellationToken)
    {
        var query = _db.Bookings
            .AsNoTracking()
            .Include(booking => booking.Room)
            .Include(booking => booking.Organizer)
            .Where(booking => booking.Start < to && from < booking.End);

        if (roomId.HasValue)
        {
            query = query.Where(booking => booking.RoomId == roomId.Value);
        }

        if (!string.IsNullOrEmpty(organizerId))
        {
            query = query.Where(booking => booking.OrganizerId == organizerId);
        }

        if (!includeCancelled)
        {
            query = query.Where(booking => booking.Status == BookingStatus.Confirmed);
        }

        return await query
            .OrderBy(booking => booking.Start)
            .ThenBy(booking => booking.Room.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<BookingMutationResult> CreateAsync(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new InvalidBookingException("Give the meeting a title.");
        }

        var title = command.Title.Trim();
        if (title.Length > 160)
        {
            throw new InvalidBookingException("Titles can be at most 160 characters.");
        }

        var notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
        if (notes is { Length: > 2000 })
        {
            throw new InvalidBookingException("Notes can be at most 2000 characters.");
        }

        var start = AsWallClock(command.Start);
        var end = AsWallClock(command.End);
        if (end <= start)
        {
            throw new InvalidBookingException("The end time has to be after the start.");
        }

        if (start.Date != end.Date)
        {
            throw new InvalidBookingException("Book the room within a single day. Overnight holds are not part of this version.");
        }

        var duration = end - start;
        if (duration < TimeSpan.FromMinutes(15))
        {
            throw new InvalidBookingException("Book at least 15 minutes.");
        }

        if (duration > TimeSpan.FromHours(12))
        {
            throw new InvalidBookingException("A single booking can't run longer than 12 hours.");
        }

        if (end <= DateTime.Now)
        {
            throw new InvalidBookingException("That time has already passed.");
        }

        var attendees = EmailAddressParser.Parse(command.AttendeeEmails);
        if (attendees.Count > 0 && string.Join(", ", attendees).Length > 2000)
        {
            throw new InvalidBookingException("The attendee list is too long.");
        }

        var room = await _db.Rooms.FirstOrDefaultAsync(item => item.Id == command.RoomId, cancellationToken);
        if (room is null)
        {
            throw new NotFoundException("That room doesn't exist.");
        }

        if (!room.IsActive)
        {
            throw new InvalidBookingException("That room is closed for booking.");
        }

        var organizer = await _db.Users.FirstOrDefaultAsync(user => user.Id == command.OrganizerId, cancellationToken);
        if (organizer is null || !organizer.IsActive)
        {
            throw new InvalidBookingException("The organizer account is not available.");
        }

        IDbContextTransaction? transaction = null;
        var committed = false;
        if (_db.Database.IsRelational())
        {
            transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        }

        try
        {
            var conflict = await _db.Bookings
                .Where(booking => booking.RoomId == room.Id && booking.Status == BookingStatus.Confirmed)
                .Where(booking => booking.Start < end && start < booking.End)
                .Select(booking => new { booking.Title, booking.Start, booking.End })
                .FirstOrDefaultAsync(cancellationToken);

            if (conflict is not null)
            {
                throw new BookingConflictException(
                    $"Already booked {conflict.Start:h:mm tt}–{conflict.End:h:mm tt} ({conflict.Title}). Pick another time or room.");
            }

            var booking = new Booking
            {
                RoomId = room.Id,
                Room = room,
                OrganizerId = organizer.Id,
                Organizer = organizer,
                Title = title,
                Notes = notes,
                Start = start,
                End = end,
                AttendeeEmails = attendees.Count == 0 ? null : string.Join(", ", attendees),
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.Now
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                committed = true;
            }

            var email = await SendSafeAsync(
                () => _email.SendBookingConfirmedAsync(ToEmailModel(booking, attendees), cancellationToken),
                booking.Id);
            return new BookingMutationResult(booking, email.Sent, email.Warning);
        }
        catch
        {
            if (transaction is not null && !committed)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<BookingMutationResult> CancelAsync(
        int bookingId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var booking = await _db.Bookings
            .Include(item => item.Room)
            .Include(item => item.Organizer)
            .FirstOrDefaultAsync(item => item.Id == bookingId, cancellationToken);

        if (booking is null)
        {
            throw new NotFoundException("That booking doesn't exist.");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidBookingException("That booking is already cancelled.");
        }

        if (!isAdmin && booking.OrganizerId != userId)
        {
            throw new ForbiddenException("You can only cancel your own reservations.");
        }

        if (booking.End <= DateTime.Now)
        {
            throw new InvalidBookingException("That meeting has already ended.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);

        var attendees = EmailAddressParser.Parse(booking.AttendeeEmails);
        var email = await SendSafeAsync(
            () => _email.SendBookingCancelledAsync(ToEmailModel(booking, attendees), cancellationToken),
            booking.Id);
        return new BookingMutationResult(booking, email.Sent, email.Warning);
    }

    private async Task<EmailSendResult> SendSafeAsync(Func<Task<EmailSendResult>> send, int bookingId)
    {
        try
        {
            return await send();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Booking {BookingId} was saved but the notification threw.", bookingId);
            return new EmailSendResult(false, "The reservation was saved, but the invitation email could not be sent.");
        }
    }

    private static BookingEmailModel ToEmailModel(Booking booking, IReadOnlyList<string> attendees)
    {
        var location = $"{booking.Room.Name} · {booking.Room.Building} · Floor {booking.Room.Floor}";
        if (!string.IsNullOrWhiteSpace(booking.Room.LocationDetail))
        {
            location += $" · {booking.Room.LocationDetail}";
        }

        return new BookingEmailModel(
            booking.Id,
            booking.Title,
            booking.Room.Name,
            location,
            booking.Start,
            booking.End,
            booking.Organizer.DisplayName,
            booking.Organizer.Email ?? string.Empty,
            booking.Notes,
            attendees);
    }

    private static DateTime AsWallClock(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
}
