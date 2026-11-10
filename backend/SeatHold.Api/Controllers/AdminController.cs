using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeatHold.Api.Data;
using SeatHold.Api.Models;

namespace SeatHold.Api.Controllers;

public record LoginRequest(string? Password);

[ApiController]
[Route("admin")]
public class AdminController(IConfiguration config, AppDbContext db) : ControllerBase
{
    private bool Authorized()
    {
        var pw = config["ADMIN_PASSWORD"];
        return string.IsNullOrEmpty(pw) || Request.Headers["X-Admin-Password"].ToString() == pw;
    }

    [HttpGet("status")]
    public IActionResult Status() =>
        Ok(new { required = !string.IsNullOrEmpty(config["ADMIN_PASSWORD"]) });

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var pw = config["ADMIN_PASSWORD"];
        if (string.IsNullOrEmpty(pw) || req.Password == pw)
            return Ok(new { ok = true });
        return Unauthorized(new { error = "পাসওয়ার্ড ভুল।" });
    }

    [HttpGet("holds")]
    public async Task<IActionResult> Holds([FromQuery] string? classId)
    {
        if (!Authorized())
            return StatusCode(401, new { error = "Admin পাসওয়ার্ড লাগবে।" });

        var now = DateTime.UtcNow;
        IQueryable<Hold> q = db.Holds.AsNoTracking();
        if (!string.IsNullOrEmpty(classId))
            q = q.Where(h => h.ClassId == classId);

        var rows = await (from h in q
                          join c in db.Classes on h.ClassId equals c.Id
                          orderby h.CreatedAt descending
                          select new { Hold = h, ClassName = c.Name }).ToListAsync();

        return Ok(rows.Select(r => new
        {
            holdId = r.Hold.Id,
            classId = r.Hold.ClassId,
            className = r.ClassName,
            studentId = r.Hold.StudentId,
            status = r.Hold.Status == HoldStatus.Held && r.Hold.ExpiresAt <= now
                ? HoldStatus.Expired
                : r.Hold.Status,
            transactionId = r.Hold.TransactionId,
            createdAt = r.Hold.CreatedAt,
            expiresAt = r.Hold.ExpiresAt
        }));
    }
}