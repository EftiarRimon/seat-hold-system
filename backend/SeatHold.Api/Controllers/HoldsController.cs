using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SeatHold.Api.Data;
using SeatHold.Api.Models;

namespace SeatHold.Api.Controllers;

public record HoldResponse(string HoldId, string ClassId, string StudentId, string Status, DateTime ExpiresAt);
public record ConfirmRequest(string? TransactionId);

[ApiController]
public class HoldsController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private static readonly Regex TxnPattern = new("^[A-Za-z0-9]{6,30}$");

    private bool TryGetStudentId(out string studentId, out string error)
    {
        studentId = Request.Headers["X-Student-Id"].FirstOrDefault()?.Trim() ?? "";
        error = "";
        if (studentId.Length == 0)
        {
            error = "জন্ম নিবন্ধন নম্বর দিন।";
            return false;
        }
        if (studentId.Length != 17 || !studentId.All(char.IsAsciiDigit))
        {
            error = "জন্ম নিবন্ধন নম্বর অবশ্যই ১৭ অঙ্কের সংখ্যা হতে হবে।";
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
        var onePerStudent = config.GetValue<bool?>("ONE_APPLICATION_PER_STUDENT") ?? true;

        await using var tx = await db.Database.BeginTransactionAsync();

        // একই শিক্ষার্থীর দুটো আবেদন একসাথে এলে একটা অপরটার জন্য অপেক্ষা করবে
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({studentId}))");

        // Class row lock: এই transaction শেষ না হওয়া পর্যন্ত অন্য request এখানে অপেক্ষা করবে
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM classes WHERE \"Id\" = {classId} FOR UPDATE");

        var cls = await db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId);
        if (cls is null)
            return NotFound(new { error = "Class খুঁজে পাওয়া যায়নি।" });

        var now = DateTime.UtcNow;

        if (onePerStudent)
        {
            var anyActive = await db.Holds.AnyAsync(h => h.StudentId == studentId
                && (h.Status == HoldStatus.Confirmed
                    || (h.Status == HoldStatus.Held && h.ExpiresAt > now)));
            if (anyActive)
                return Conflict(new { error = "এই জন্ম নিবন্ধন নম্বর দিয়ে আগে থেকেই একটি আবেদন বা নিশ্চিত ভর্তি আছে। একজন শিক্ষার্থী একটির বেশি আবেদন করতে পারবে না।" });
        }
        else
        {
            var alreadyActive = await db.Holds.AnyAsync(h => h.ClassId == classId
                && h.StudentId == studentId
                && (h.Status == HoldStatus.Confirmed
                    || (h.Status == HoldStatus.Held && h.ExpiresAt > now)));
            if (alreadyActive)
                return Conflict(new { error = "এই class-এ আপনার আগে থেকেই একটি চলমান আবেদন বা নিশ্চিত ভর্তি আছে।" });
        }

        var taken = await db.Holds.CountAsync(h => h.ClassId == classId
            && (h.Status == HoldStatus.Confirmed
                || (h.Status == HoldStatus.Held && h.ExpiresAt > now)));
        if (taken >= cls.TotalSeats)
            return Conflict(new { error = "দুঃখিত, এই class-এ আর কোনো seat খালি নেই।" });

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
            return NotFound(new { error = "আবেদন খুঁজে পাওয়া যায়নি।" });

        return Ok(new
        {
            holdId = h.Id,
            classId = h.ClassId,
            status = EffectiveStatus(h, DateTime.UtcNow),
            expiresAt = h.ExpiresAt
        });
    }

    [HttpPost("holds/{holdId}/confirm")]
    public async Task<IActionResult> Confirm(
        string holdId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ConfirmRequest? req = null)
    {
        if (!TryGetStudentId(out var studentId, out var error))
            return BadRequest(new { error });

        var txn = req?.TransactionId?.Trim();
        if (string.IsNullOrEmpty(txn)) txn = null;
        if (txn is not null && !TxnPattern.IsMatch(txn))
            return BadRequest(new { error = "Transaction নম্বরে শুধু ইংরেজি অক্ষর ও সংখ্যা থাকবে (৬ থেকে ৩০ অক্ষর)।" });

        var peek = await db.Holds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == holdId);
        if (peek is null)
            return NotFound(new { error = "আবেদন খুঁজে পাওয়া যায়নি।" });

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM classes WHERE \"Id\" = {peek.ClassId} FOR UPDATE");

        var hold = await db.Holds.FirstAsync(x => x.Id == holdId);
        if (hold.StudentId != studentId)
            return StatusCode(403, new { error = "এই আবেদন আপনার নয়।" });

        if (hold.Status == HoldStatus.Confirmed)
            return Ok(new { holdId = hold.Id, status = hold.Status, transactionId = hold.TransactionId });

        if (hold.Status != HoldStatus.Held)
            return Conflict(new { error = "এই আবেদন আর নিশ্চিত করা যাবে না (বাতিল বা মেয়াদ শেষ)।" });

        if (hold.ExpiresAt <= DateTime.UtcNow)
            return Conflict(new { error = "আবেদনের মেয়াদ শেষ হয়ে গেছে, ভর্তি ফি দেওয়া যাবে না।" });

        hold.Status = HoldStatus.Confirmed;
        hold.TransactionId = txn;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "এই Transaction নম্বর আগে ব্যবহার করা হয়েছে।" });
        }
        await tx.CommitAsync();

        return Ok(new { holdId = hold.Id, status = hold.Status, transactionId = hold.TransactionId });
    }

    [HttpDelete("holds/{holdId}")]
    public async Task<IActionResult> Cancel(string holdId)
    {
        if (!TryGetStudentId(out var studentId, out var error))
            return BadRequest(new { error });

        var hold = await db.Holds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == holdId);
        if (hold is null)
            return NotFound(new { error = "আবেদন খুঁজে পাওয়া যায়নি।" });
        if (hold.StudentId != studentId)
            return StatusCode(403, new { error = "এই আবেদন আপনার নয়।" });
        if (hold.Status == HoldStatus.Confirmed)
            return Conflict(new { error = "নিশ্চিত ভর্তি বাতিল করা যাবে না।" });

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
            expiresAt = r.Hold.ExpiresAt,
            transactionId = r.Hold.TransactionId
        }));
    }
}