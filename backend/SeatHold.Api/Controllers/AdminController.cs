using Microsoft.AspNetCore.Mvc;

namespace SeatHold.Api.Controllers;

public record LoginRequest(string? Password);

[ApiController]
[Route("admin")]
public class AdminController(IConfiguration config) : ControllerBase
{
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
}