using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MRRS.Web.Models;

namespace MRRS.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(120);
            entity.Property(user => user.Department).HasMaxLength(80);
        });

        builder.Entity<Room>(entity =>
        {
            entity.Property(room => room.Name).HasMaxLength(120).IsRequired();
            entity.Property(room => room.Building).HasMaxLength(80).IsRequired();
            entity.Property(room => room.Floor).HasMaxLength(40).IsRequired();
            entity.Property(room => room.LocationDetail).HasMaxLength(200);
            entity.Property(room => room.AvNotes).HasMaxLength(1000);
            entity.HasIndex(room => new { room.Building, room.Name });
        });

        builder.Entity<Booking>(entity =>
        {
            entity.Property(booking => booking.Title).HasMaxLength(160).IsRequired();
            entity.Property(booking => booking.Notes).HasMaxLength(2000);
            entity.Property(booking => booking.AttendeeEmails).HasMaxLength(2000);
            entity.Property(booking => booking.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(booking => new { booking.RoomId, booking.Status, booking.Start });
            entity.HasOne(booking => booking.Room)
                .WithMany(room => room.Bookings)
                .HasForeignKey(booking => booking.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(booking => booking.Organizer)
                .WithMany(user => user.Bookings)
                .HasForeignKey(booking => booking.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
