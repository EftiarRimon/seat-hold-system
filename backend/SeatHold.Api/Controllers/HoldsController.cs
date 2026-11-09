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
}