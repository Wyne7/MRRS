namespace MRRS.Web.Models;

public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public string OrganizerId { get; set; } = string.Empty;
    public ApplicationUser Organizer { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? AttendeeEmails { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public enum BookingStatus
{
    Confirmed = 0,
    Cancelled = 1
}
