using Microsoft.AspNetCore.Mvc;
using SeatHold.Api.Data;
using SeatHold.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SeatHold.Api.Controllers;

public record CreateClassRequest(string? Name, int? TotalSeats);
public record ClassResponse(string Id, string Name, int TotalSeats);
public record ClassSeatsResponse(
    string Id, string Name, int TotalSeats,
    int HeldSeats, int ConfirmedSeats, int AvailableSeats);

[ApiController]
[Route("classes")]
public class ClassesController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest req)
    {
        var name = req.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest(new { error = "Class-er nam dewa lagbe." });
        if (req.TotalSeats is null or <= 0)
            return BadRequest(new { error = "Seat sonkha 0-er beshi hote hobe." });

        var c = new SchoolClass { Name = name, TotalSeats = req.TotalSeats.Value };
        db.Classes.Add(c);
        await db.SaveChangesAsync();

        return Created($"/classes/{c.Id}", new ClassResponse(c.Id, c.Name, c.TotalSeats));
    }
        [HttpGet]
    public async Task<IActionResult> List()
    {
        var now = DateTime.UtcNow;
        var rows = await db.Classes
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id, c.Name, c.TotalSeats,
                Held = db.Holds.Count(h => h.ClassId == c.Id
                    && h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = db.Holds.Count(h => h.ClassId == c.Id
                    && h.Status == HoldStatus.Confirmed)
            })
            .ToListAsync();

        return Ok(rows.Select(r => new ClassSeatsResponse(
            r.Id, r.Name, r.TotalSeats, r.Held, r.Confirmed,
            r.TotalSeats - r.Held - r.Confirmed)));
    }

    [HttpGet("{classId}")]
    public async Task<IActionResult> Get(string classId)
    {
        var now = DateTime.UtcNow;
        var c = await db.Classes.FirstOrDefaultAsync(x => x.Id == classId);
        if (c is null)
            return NotFound(new { error = "Class paoya jayni." });

        var held = await db.Holds.CountAsync(h => h.ClassId == classId
            && h.Status == HoldStatus.Held && h.ExpiresAt > now);
        var confirmed = await db.Holds.CountAsync(h => h.ClassId == classId
            && h.Status == HoldStatus.Confirmed);

        return Ok(new ClassSeatsResponse(
            c.Id, c.Name, c.TotalSeats, held, confirmed, c.TotalSeats - held - confirmed));
    }
}