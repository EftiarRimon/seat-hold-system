namespace SeatHold.Api.Models;

public class SchoolClass
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public int TotalSeats { get; set; }
}