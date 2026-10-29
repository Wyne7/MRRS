using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRRS.Web.Contracts;
using MRRS.Web.Data;
using MRRS.Web.Models;

namespace MRRS.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public RoomsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> List([FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var query = _db.Rooms.AsNoTracking().AsQueryable();
        var canSeeInactive = includeInactive && User.IsInRole(AppRoles.Admin);
        if (!canSeeInactive)
        {
            query = query.Where(room => room.IsActive);
        }

        var rooms = await query
            .OrderBy(room => room.Building)
            .ThenBy(room => room.Floor)
            .ThenBy(room => room.Name)
            .ToListAsync(cancellationToken);

        return Ok(rooms.Select(ApiMapper.ToDto).ToArray());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> Get(int id, CancellationToken cancellationToken)
    {
        var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (room is null || (!room.IsActive && !User.IsInRole(AppRoles.Admin)))
        {
            return NotFound(new { message = "That room doesn't exist." });
        }

        return Ok(ApiMapper.ToDto(room));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(RoomWriteRequest request, CancellationToken cancellationToken)
    {
        var error = Validate(request);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var room = new Room();
        Apply(room, request);
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiMapper.ToDto(room));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomDto>> Update(int id, RoomWriteRequest request, CancellationToken cancellationToken)
    {
        var error = Validate(request);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var room = await _db.Rooms.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (room is null)
        {
            return NotFound(new { message = "That room doesn't exist." });
        }

        Apply(room, request);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiMapper.ToDto(room));
    }

    private static string? Validate(RoomWriteRequest request)
    {
        if (!ModelIsComplete(request))
        {
            return "Name, building, and floor are required.";
        }

        if (request.Capacity < 1 || request.Capacity > 500)
        {
            return "Capacity must be between 1 and 500.";
        }

        return null;
    }

    private static bool ModelIsComplete(RoomWriteRequest request) =>
        !string.IsNullOrWhiteSpace(request.Name)
        && !string.IsNullOrWhiteSpace(request.Building)
        && !string.IsNullOrWhiteSpace(request.Floor);

    private static void Apply(Room room, RoomWriteRequest request)
    {
        room.Name = request.Name.Trim();
        room.Building = request.Building.Trim();
        room.Floor = request.Floor.Trim();
        room.LocationDetail = string.IsNullOrWhiteSpace(request.LocationDetail) ? null : request.LocationDetail.Trim();
        room.Capacity = request.Capacity;
        room.HasProjector = request.HasProjector;
        room.HasVideoConference = request.HasVideoConference;
        room.HasWhiteboard = request.HasWhiteboard;
        room.HasConferencePhone = request.HasConferencePhone;
        room.AvNotes = string.IsNullOrWhiteSpace(request.AvNotes) ? null : request.AvNotes.Trim();
        room.IsActive = request.IsActive;
    }
}
