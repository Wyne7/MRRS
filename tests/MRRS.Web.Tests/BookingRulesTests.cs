using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MRRS.Web.Data;
using MRRS.Web.Models;
using MRRS.Web.Services;

namespace MRRS.Web.Tests;

public class BookingOverlapTests
{
    [Theory]
    [InlineData("10:00", "11:00", "10:30", "11:30", true)]
    [InlineData("10:00", "12:00", "10:30", "11:00", true)]
    [InlineData("10:00", "11:00", "09:30", "10:30", true)]
    [InlineData("10:00", "11:00", "11:00", "12:00", false)]
    [InlineData("10:00", "11:00", "08:00", "09:00", false)]
    [InlineData("10:00", "11:00", "11:01", "12:00", false)]
    public void Detects_overlap_and_allows_back_to_back(string start, string end, string otherStart, string otherEnd, bool overlaps)
    {
        var day = new DateTime(2026, 10, 29);
        Assert.Equal(
            overlaps,
            BookingOverlap.Overlaps(At(day, start), At(day, end), At(day, otherStart), At(day, otherEnd)));
    }

    private static DateTime At(DateTime day, string time)
    {
        var parts = time.Split(':');
        return day.AddHours(int.Parse(parts[0])).AddMinutes(int.Parse(parts[1]));
    }
}

public class EmailAddressParserTests
{
    [Fact]
    public void Splits_and_dedupes_addresses()
    {
        var emails = EmailAddressParser.Parse("a@mrrs.local, a@mrrs.local; b@mrrs.local\n");
        Assert.Equal(["a@mrrs.local", "b@mrrs.local"], emails);
    }

    [Fact]
    public void Rejects_a_bad_address()
    {
        var error = Assert.Throws<InvalidBookingException>(() => EmailAddressParser.Parse("not-an-email"));
        Assert.Contains("not-an-email", error.Message);
    }
}

public class PasswordHashTests
{
    [Fact]
    public void Identity_hasher_does_not_store_the_password()
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
        var user = new ApplicationUser { UserName = "admin" };
        var hash = hasher.HashPassword(user, DbInitializer.AdminPassword);

        Assert.NotEqual(DbInitializer.AdminPassword, hash);
        Assert.StartsWith("AQAAAA", hash);
        Assert.Equal(
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(user, hash, DbInitializer.AdminPassword));
    }
}

public class SmtpNotConfiguredTests
{
    [Fact]
    public async Task Empty_host_skips_the_send()
    {
        var service = new SmtpEmailNotificationService(
            Options.Create(new SmtpOptions { Host = "", FromAddress = "mrrs@localhost" }),
            NullLogger<SmtpEmailNotificationService>.Instance);

        var result = await service.SendBookingConfirmedAsync(SampleModel(), CancellationToken.None);

        Assert.False(result.Sent);
        Assert.Contains("not configured", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ics_invite_uses_request_and_cancel_methods()
    {
        var model = SampleModel();
        var request = IcsInviteBuilder.Build(model, cancel: false);
        var cancel = IcsInviteBuilder.Build(model, cancel: true);

        Assert.Contains("METHOD:REQUEST", request);
        Assert.Contains("METHOD:CANCEL", cancel);
        Assert.Contains("UID:booking-42@mrrs.local", request);
        Assert.Contains("DTSTART:20261030T100000", request);
    }

    private static BookingEmailModel SampleModel() => new(
        42,
        "Sprint planning",
        "Cedar Conference",
        "Cedar Conference · Building A · Floor 2",
        new DateTime(2026, 10, 30, 10, 0, 0),
        new DateTime(2026, 10, 30, 11, 0, 0),
        "Sample Employee",
        "employee@mrrs.local",
        "Bring the board.",
        ["admin@mrrs.local"]);
}

public class BookingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly RecordingEmail _email = new();
    private readonly BookingService _service;
    private readonly ApplicationUser _employee;
    private readonly ApplicationUser _other;
    private readonly Room _cedar;
    private readonly Room _oak;
    private readonly Room _closed;

    public BookingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _employee = User("emp-1", "employee");
        _other = User("emp-2", "other");
        _cedar = Room("Cedar Conference", active: true);
        _oak = Room("Oak Boardroom", active: true);
        _closed = Room("Closed Lab", active: false);
        _db.Users.AddRange(_employee, _other);
        _db.Rooms.AddRange(_cedar, _oak, _closed);
        _db.SaveChanges();
        _service = new BookingService(_db, _email, NullLogger<BookingService>.Instance);
    }

    [Fact]
    public async Task Creates_a_booking_and_asks_for_an_invitation()
    {
        var result = await Create(_employee.Id, _cedar.Id, 10, 11);

        Assert.Equal(BookingStatus.Confirmed, result.Booking.Status);
        Assert.True(result.EmailSent);
        Assert.Equal(1, _email.Confirmed);
        Assert.Equal(1, await _db.Bookings.CountAsync());
    }

    [Fact]
    public async Task Rejects_an_overlapping_booking_on_the_same_room()
    {
        await Create(_employee.Id, _cedar.Id, 10, 11);

        var error = await Assert.ThrowsAsync<BookingConflictException>(() => Create(_other.Id, _cedar.Id, 10, 12));

        Assert.Contains("Already booked", error.Message);
        Assert.Equal(1, await _db.Bookings.CountAsync(booking => booking.Status == BookingStatus.Confirmed));
    }

    [Fact]
    public async Task Allows_a_booking_that_starts_when_the_previous_one_ends()
    {
        await Create(_employee.Id, _cedar.Id, 10, 11);
        var next = await Create(_other.Id, _cedar.Id, 11, 12);

        Assert.Equal(11, next.Booking.Start.Hour);
    }

    [Fact]
    public async Task Allows_the_same_time_in_a_different_room()
    {
        await Create(_employee.Id, _cedar.Id, 10, 11);
        var otherRoom = await Create(_other.Id, _oak.Id, 10, 11);

        Assert.Equal(_oak.Id, otherRoom.Booking.RoomId);
    }

    [Fact]
    public async Task A_cancelled_booking_does_not_keep_the_room()
    {
        var first = await Create(_employee.Id, _cedar.Id, 10, 11);
        await _service.CancelAsync(first.Booking.Id, _employee.Id, isAdmin: false, CancellationToken.None);

        var again = await Create(_other.Id, _cedar.Id, 10, 11);

        Assert.Equal(BookingStatus.Confirmed, again.Booking.Status);
        Assert.Equal(1, _email.Cancelled);
    }

    [Fact]
    public async Task Employees_cannot_cancel_someone_elses_booking()
    {
        var first = await Create(_employee.Id, _cedar.Id, 10, 11);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.CancelAsync(first.Booking.Id, _other.Id, isAdmin: false, CancellationToken.None));

        var stored = await _db.Bookings.SingleAsync();
        Assert.Equal(BookingStatus.Confirmed, stored.Status);
    }

    [Fact]
    public async Task Admins_can_cancel_any_booking()
    {
        var first = await Create(_employee.Id, _cedar.Id, 10, 11);

        var cancelled = await _service.CancelAsync(first.Booking.Id, _other.Id, isAdmin: true, CancellationToken.None);

        Assert.Equal(BookingStatus.Cancelled, cancelled.Booking.Status);
    }

    [Fact]
    public async Task Closed_rooms_cannot_be_booked()
    {
        await Assert.ThrowsAsync<InvalidBookingException>(() => Create(_employee.Id, _closed.Id, 10, 11));
    }

    [Fact]
    public async Task End_must_be_after_start()
    {
        await Assert.ThrowsAsync<InvalidBookingException>(() => Create(_employee.Id, _cedar.Id, 11, 10));
    }

    [Fact]
    public async Task Unconfigured_smtp_still_saves_the_booking()
    {
        var smtp = new SmtpEmailNotificationService(
            Options.Create(new SmtpOptions { Host = "" }),
            NullLogger<SmtpEmailNotificationService>.Instance);
        var service = new BookingService(_db, smtp, NullLogger<BookingService>.Instance);

        var result = await service.CreateAsync(Command(_employee.Id, _cedar.Id, 13, 14), CancellationToken.None);

        Assert.False(result.EmailSent);
        Assert.Contains("not configured", result.EmailWarning, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await _db.Bookings.CountAsync());
    }

    public void Dispose() => _db.Dispose();

    private Task<BookingMutationResult> Create(string userId, int roomId, int startHour, int endHour) =>
        _service.CreateAsync(Command(userId, roomId, startHour, endHour), CancellationToken.None);

    private static CreateBookingCommand Command(string userId, int roomId, int startHour, int endHour)
    {
        var day = DateTime.Today.AddDays(3);
        return new CreateBookingCommand(
            roomId,
            userId,
            "Planning",
            null,
            day.AddHours(startHour),
            day.AddHours(endHour),
            "guest@mrrs.local");
    }

    private static ApplicationUser User(string id, string name) => new()
    {
        Id = id,
        UserName = name,
        NormalizedUserName = name.ToUpperInvariant(),
        Email = $"{name}@mrrs.local",
        NormalizedEmail = $"{name}@MRRS.LOCAL",
        DisplayName = name,
        IsActive = true,
        SecurityStamp = Guid.NewGuid().ToString(),
        ConcurrencyStamp = Guid.NewGuid().ToString()
    };

    private static Room Room(string name, bool active) => new()
    {
        Name = name,
        Building = "Building A",
        Floor = "2",
        Capacity = 8,
        IsActive = active
    };

    private sealed class RecordingEmail : IEmailNotificationService
    {
        public int Confirmed { get; private set; }
        public int Cancelled { get; private set; }

        public Task<EmailSendResult> SendBookingConfirmedAsync(BookingEmailModel model, CancellationToken cancellationToken)
        {
            Confirmed++;
            return Task.FromResult(new EmailSendResult(true, null));
        }

        public Task<EmailSendResult> SendBookingCancelledAsync(BookingEmailModel model, CancellationToken cancellationToken)
        {
            Cancelled++;
            return Task.FromResult(new EmailSendResult(true, null));
        }
    }
}
