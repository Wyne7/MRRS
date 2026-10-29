using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MRRS.Web.Models;

namespace MRRS.Web.Data;

public static class DbInitializer
{
    public const string AdminUserName = "admin";
    public const string AdminPassword = "Admin123!";
    public const string AdminEmail = "admin@mrrs.local";
    public const string EmployeeUserName = "employee";
    public const string EmployeePassword = "Employee123!";
    public const string EmployeeEmail = "employee@mrrs.local";

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("MRRS.Web.Data.DbInitializer");
        var db = provider.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var created = await roleManager.CreateAsync(new IdentityRole(role));
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(FormatErrors(created));
                }
            }
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, new ApplicationUser
        {
            UserName = AdminUserName,
            Email = AdminEmail,
            EmailConfirmed = true,
            DisplayName = "Facilities Admin",
            Department = "Facilities",
            IsActive = true
        }, AdminPassword, AppRoles.Admin);

        var employee = await EnsureUserAsync(userManager, new ApplicationUser
        {
            UserName = EmployeeUserName,
            Email = EmployeeEmail,
            EmailConfirmed = true,
            DisplayName = "Sample Employee",
            Department = "Product",
            IsActive = true
        }, EmployeePassword, AppRoles.Employee);

        if (!await db.Rooms.AnyAsync(cancellationToken))
        {
            db.Rooms.AddRange(
                new Room
                {
                    Name = "Cedar Conference",
                    Building = "Building A",
                    Floor = "2",
                    LocationDetail = "East wing, room 214",
                    Capacity = 12,
                    HasProjector = true,
                    HasVideoConference = true,
                    AvNotes = "Ceiling speakerphone. HDMI at the table.",
                    IsActive = true
                },
                new Room
                {
                    Name = "Birch Huddle",
                    Building = "Building A",
                    Floor = "2",
                    LocationDetail = "East wing, room 206",
                    Capacity = 4,
                    HasWhiteboard = true,
                    AvNotes = "No display. Bring a laptop if you need one.",
                    IsActive = true
                },
                new Room
                {
                    Name = "Oak Boardroom",
                    Building = "Building B",
                    Floor = "10",
                    LocationDetail = "Room 1002",
                    Capacity = 20,
                    HasProjector = true,
                    HasVideoConference = true,
                    HasWhiteboard = true,
                    HasConferencePhone = true,
                    AvNotes = "Crestron panel by the door. USB-C and HDMI in the table cubby.",
                    IsActive = true
                },
                new Room
                {
                    Name = "Maple Interview",
                    Building = "Building A",
                    Floor = "1",
                    LocationDetail = "Lobby, room 108",
                    Capacity = 2,
                    HasVideoConference = true,
                    AvNotes = "Single display and a camera pointed at the chair by the window.",
                    IsActive = true
                });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Bookings.AnyAsync(cancellationToken))
        {
            var admin = await userManager.FindByNameAsync(AdminUserName);
            var cedar = await db.Rooms.SingleAsync(room => room.Name == "Cedar Conference", cancellationToken);
            var oak = await db.Rooms.SingleAsync(room => room.Name == "Oak Boardroom", cancellationToken);
            var sampleStart = NextOpenSlot();

            db.Bookings.AddRange(
                new Booking
                {
                    RoomId = cedar.Id,
                    OrganizerId = employee.Id,
                    Title = "Sprint planning",
                    Notes = "Bring the board from last week.",
                    Start = sampleStart,
                    End = sampleStart.AddHours(1),
                    AttendeeEmails = AdminEmail,
                    Status = BookingStatus.Confirmed,
                    CreatedAt = DateTime.Now
                },
                new Booking
                {
                    RoomId = oak.Id,
                    OrganizerId = admin!.Id,
                    Title = "Quarterly review",
                    Start = sampleStart.Date.AddDays(1).AddHours(14),
                    End = sampleStart.Date.AddDays(1).AddHours(15).AddMinutes(30),
                    Status = BookingStatus.Confirmed,
                    CreatedAt = DateTime.Now
                });
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "MRRS database is ready. Seeded sign-in names are {Admin} and {Employee}.",
            AdminUserName,
            EmployeeUserName);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password,
        string role)
    {
        var existing = await userManager.FindByNameAsync(user.UserName!);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }

            return existing;
        }

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(FormatErrors(created));
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(FormatErrors(roleResult));
        }

        return user;
    }

    private static DateTime NextOpenSlot()
    {
        var start = DateTime.Now.AddHours(2);
        start = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
        if (start.Hour < 8)
        {
            start = DateTime.Today.AddHours(10);
        }
        else if (start.Hour > 16)
        {
            start = DateTime.Today.AddDays(1).AddHours(10);
        }

        return start;
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));
}
