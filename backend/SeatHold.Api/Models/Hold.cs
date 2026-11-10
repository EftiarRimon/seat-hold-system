namespace SeatHold.Api.Models;

public static class HoldStatus
{
    public const string Held = "HELD";
    public const string Confirmed = "CONFIRMED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";
}

public class Hold
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClassId { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string Status { get; set; } = HoldStatus.Held;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TransactionId { get; set; }
}