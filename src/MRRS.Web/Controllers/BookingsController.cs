using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRRS.Web.Contracts;
using MRRS.Web.Models;
using MRRS.Web.Services;

namespace MRRS.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookings;

    public BookingsController(BookingService bookings)
    {
        _bookings = bookings;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> List(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int? roomId,
        [FromQuery] bool mine,
        [FromQuery] bool includeCancelled,
        CancellationToken cancellationToken)
    {
        from = DateTime.SpecifyKind(from, DateTimeKind.Unspecified);
        to = DateTime.SpecifyKind(to, DateTimeKind.Unspecified);
        if (from == default || to == default || to <= from)
        {
            return BadRequest(new { message = "Provide a from and to range, with to after from." });
        }

        if (to - from > TimeSpan.FromDays(366))
        {
            return BadRequest(new { message = "Ask for a year or less at a time." });
        }

        var userId = CurrentUserId();
        var isAdmin = User.IsInRole(AppRoles.Admin);
        var bookings = await _bookings.SearchAsync(
            from,
            to,
            roomId,
            mine ? userId : null,
            includeCancelled,
            cancellationToken);

        return Ok(bookings.Select(booking => ApiMapper.ToDto(booking, userId, isAdmin)).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<BookingMutationResponse>> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Check the reservation and try again." });
        }

        var result = await _bookings.CreateAsync(new CreateBookingCommand(
            request.RoomId,
            CurrentUserId(),
            request.Title,
            request.Notes,
            request.Start,
            request.End,
            request.AttendeeEmails), cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<BookingMutationResponse>> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await _bookings.CancelAsync(id, CurrentUserId(), User.IsInRole(AppRoles.Admin), cancellationToken);
        return Ok(ToResponse(result));
    }

    private BookingMutationResponse ToResponse(BookingMutationResult result)
    {
        var userId = CurrentUserId();
        return new BookingMutationResponse(
            ApiMapper.ToDto(result.Booking, userId, User.IsInRole(AppRoles.Admin)),
            result.EmailSent,
            result.EmailWarning);
    }

    private string CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new ForbiddenException("Sign in to continue.");
}
