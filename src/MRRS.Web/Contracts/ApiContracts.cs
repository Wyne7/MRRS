using System.ComponentModel.DataAnnotations;
using MRRS.Web.Models;

namespace MRRS.Web.Contracts;

public class LoginRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record UserDto(
    string Id,
    string UserName,
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    IReadOnlyList<string> Roles);

public class UserWriteRequest
{
    [MaxLength(40)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Department { get; set; }

    public string? Password { get; set; }

    [Required]
    public string Role { get; set; } = AppRoles.Employee;

    public bool IsActive { get; set; } = true;
}

public record RoomDto(
    int Id,
    string Name,
    string Building,
    string Floor,
    string? LocationDetail,
    int Capacity,
    bool HasProjector,
    bool HasVideoConference,
    bool HasWhiteboard,
    bool HasConferencePhone,
    string? AvNotes,
    bool IsActive);

public class RoomWriteRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Building { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Floor { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? LocationDetail { get; set; }

    public int Capacity { get; set; }

    public bool HasProjector { get; set; }
    public bool HasVideoConference { get; set; }
    public bool HasWhiteboard { get; set; }
    public bool HasConferencePhone { get; set; }

    [MaxLength(1000)]
    public string? AvNotes { get; set; }

    public bool IsActive { get; set; } = true;
}

public record BookingDto(
    int Id,
    int RoomId,
    string RoomName,
    string Building,
    string Floor,
    string? LocationDetail,
    string OrganizerId,
    string OrganizerName,
    string Title,
    string? Notes,
    DateTime Start,
    DateTime End,
    string? AttendeeEmails,
    string Status,
    bool CanCancel);

public record BookingMutationResponse(BookingDto Booking, bool EmailSent, string? EmailWarning);

public class CreateBookingRequest
{
    public int RoomId { get; set; }

    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    [MaxLength(2000)]
    public string? AttendeeEmails { get; set; }
}

public static class ApiMapper
{
    public static RoomDto ToDto(Room room) => new(
        room.Id,
        room.Name,
        room.Building,
        room.Floor,
        room.LocationDetail,
        room.Capacity,
        room.HasProjector,
        room.HasVideoConference,
        room.HasWhiteboard,
        room.HasConferencePhone,
        room.AvNotes,
        room.IsActive);

    public static BookingDto ToDto(Booking booking, string currentUserId, bool isAdmin)
    {
        var canCancel = booking.Status == BookingStatus.Confirmed
            && booking.End > DateTime.Now
            && (isAdmin || booking.OrganizerId == currentUserId);

        return new BookingDto(
            booking.Id,
            booking.RoomId,
            booking.Room.Name,
            booking.Room.Building,
            booking.Room.Floor,
            booking.Room.LocationDetail,
            booking.OrganizerId,
            string.IsNullOrWhiteSpace(booking.Organizer.DisplayName)
                ? booking.Organizer.UserName ?? "Organizer"
                : booking.Organizer.DisplayName,
            booking.Title,
            booking.Notes,
            booking.Start,
            booking.End,
            booking.AttendeeEmails,
            booking.Status.ToString(),
            canCancel);
    }

    public static UserDto ToDto(ApplicationUser user, IReadOnlyList<string> roles) => new(
        user.Id,
        user.UserName ?? string.Empty,
        user.Email ?? string.Empty,
        user.DisplayName,
        user.Department,
        user.IsActive,
        roles);
}
