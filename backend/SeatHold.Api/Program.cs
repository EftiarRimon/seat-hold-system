using Microsoft.EntityFrameworkCore;
using SeatHold.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")
        ?? "Host=localhost;Port=5433;Database=seats;Username=app;Password=app"));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors();
app.MapControllers();

app.Run();

public partial class Program { }