namespace MRRS.Web.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string? LocationDetail { get; set; }
    public int Capacity { get; set; }
    public bool HasProjector { get; set; }
    public bool HasVideoConference { get; set; }
    public bool HasWhiteboard { get; set; }
    public bool HasConferencePhone { get; set; }
    public string? AvNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
