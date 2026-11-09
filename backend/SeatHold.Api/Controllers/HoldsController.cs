using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeatHold.Api.Data;
using SeatHold.Api.Models;

namespace SeatHold.Api.Controllers;

public record HoldResponse(string HoldId, string ClassId, string StudentId, string Status, DateTime ExpiresAt);

[ApiController]
public class HoldsController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private bool TryGetStudentId(out string studentId, out string error)
    {
        studentId = Request.Headers["X-Student-Id"].FirstOrDefault()?.Trim() ?? "";
        error = "";
        if (studentId.Length == 0)
        {
            error = "X-Student-Id header dewa lagbe.";
            return false;
        }
        if (studentId.Length != 17 || !studentId.All(char.IsAsciiDigit))
        {
            error = "Student ID obosshoi 17 digit-er sonkha hote hobe.";
            return false;
        }
        return true;
    }

    private static string EffectiveStatus(Hold h, DateTime now) =>
        h.Status == HoldStatus.Held && h.ExpiresAt <= now ? HoldStatus.Expired : h.Status;

    [HttpPost("classes/{classId}/holds")]
    public async Task<IActionResult> CreateHold(string classId)
    {
        if (!TryGetStudentId(out var studentId, out var error))
            return BadRequest(new { error });

        var duration = config.GetValue<int?>("HOLD_DURATION_SECONDS") ?? 600;
        if (duration <= 0) duration = 600;

        await using var tx = await db.Database.BeginTransactionAsync();

        // Class row lock: ei transaction shesh na hoa porjonto onno request ekhane wait korbe
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM classes WHERE \"Id\" = {classId} FOR UPDATE");

        var cls = await db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId);
        if (cls is null)
            return NotFound(new { error = "Class paoya jayni." });

        var now = DateTime.UtcNow;

        var alreadyActive = await db.Holds.AnyAsync(h => h.ClassId == classId
            && h.StudentId == studentId
            && (h.Status == HoldStatus.Confirmed
                || (h.Status == HoldStatus.Held && h.ExpiresAt > now)));
        if (alreadyActive)
            return Conflict(new { error = "Ei class-e apnar already ekta active abedon ba nishchit bhorti ache." });

        var taken = await db.Holds.CountAsync(h => h.ClassId == classId
            && (h.Status == HoldStatus.Confirmed
                || (h.Status == HoldStatus.Held && h.ExpiresAt > now)));
        if (taken >= cls.TotalSeats)
            return Conflict(new { error = "Ei class-e ar kono seat khali nai." });

        var hold = new Hold
        {
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(duration)
        };
        db.Holds.Add(hold);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Created($"/holds/{hold.Id}",
            new HoldResponse(hold.Id, hold.ClassId, hold.StudentId, hold.Status, hold.ExpiresAt));
    }

    [HttpGet("holds/{holdId}")]
    public async Task<IActionResult> GetHold(string holdId)
    {
        var h = await db.Holds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == holdId);
        if (h is null)
            return NotFound(new { error = "Abedon paoya jayni." });

        return Ok(new
        {
            holdId = h.Id,
            classId = h.ClassId,
            status = EffectiveStatus(h, DateTime.UtcNow),
            expiresAt = h.ExpiresAt
        });
    }

    [HttpPost("holds/{holdId}/confirm")]
    public async Task<IActionResult> Confirm(string holdId)
    {
        if (!TryGetStudentId(out var studentId, out var error))
            return BadRequest(new { error });

        var peek = await db.Holds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == holdId);
        if (peek is null)
            return NotFound(new { error = "Abedon paoya jayni." });

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM classes WHERE \"Id\" = {peek.ClassId} FOR UPDATE");

        var hold = await db.Holds.FirstAsync(x => x.Id == holdId);
        if (hold.StudentId != studentId)
            return StatusCode(403, new { error = "Ei abedon apnar na." });

        if (hold.Status == HoldStatus.Confirmed)
            return Ok(new { holdId = hold.Id, status = hold.Status });

        if (hold.Status != HoldStatus.Held)
            return Conflict(new { error = "Ei abedon ar confirm kora jabe na." });

        if (hold.ExpiresAt <= DateTime.UtcNow)
            return Conflict(new { error = "Abedoner meyad shesh hoye geche." });

        hold.Status = HoldStatus.Confirmed;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { holdId = hold.Id, status = hold.Status });
    }

    [HttpDelete("holds/{holdId}")]
    public async Task<IActionResult> Cancel(string holdId)
    {
        if (!TryGetStudentId(out var studentId, out var error))
            return BadRequest(new { error });

        var hold = await db.Holds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == holdId);
        if (hold is null)
            return NotFound(new { error = "Abedon paoya jayni." });
        if (hold.StudentId != studentId)
            return StatusCode(403, new { error = "Ei abedon apnar na." });
        if (hold.Status == HoldStatus.Confirmed)
            return Conflict(new { error = "Nishchit bhorti batil kora jabe na." });

        var now = DateTime.UtcNow;
        await db.Holds
            .Where(h => h.Id == holdId && h.Status == HoldStatus.Held && h.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Cancelled));

        return NoContent();
    }

    [HttpGet("me/holds")]
    public async Task<IActionResult> MyHolds()
    {
        if (!TryGetStudentId(out var studentId, out var error))
            return BadRequest(new { error });

        var now = DateTime.UtcNow;
        var rows = await (from h in db.Holds.AsNoTracking()
                          join c in db.Classes on h.ClassId equals c.Id
                          where h.StudentId == studentId
                          orderby h.CreatedAt descending
                          select new { Hold = h, ClassName = c.Name }).ToListAsync();

        return Ok(rows.Select(r => new
        {
            holdId = r.Hold.Id,
            classId = r.Hold.ClassId,
            className = r.ClassName,
            status = EffectiveStatus(r.Hold, now),
            expiresAt = r.Hold.ExpiresAt
        }));
    }
}