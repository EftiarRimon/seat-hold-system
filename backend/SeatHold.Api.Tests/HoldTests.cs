using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SeatHold.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        var cs = Environment.GetEnvironmentVariable("TEST_DB")
            ?? "Host=localhost;Port=5433;Database=seats_test;Username=app;Password=app";
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", cs);
        Environment.SetEnvironmentVariable("HOLD_DURATION_SECONDS", "600");
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", "");
    }
}

public class HoldTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly long RunBase =
        10_000_000_000_000_000L + Random.Shared.NextInt64(0, 80_000_000_000_000_000L);

    private static string Sid(int n) => (RunBase + n).ToString();

    private async Task<string> CreateClass(int seats)
    {
        var res = await _client.PostAsJsonAsync("/classes",
            new { name = $"T-{Guid.NewGuid():N}", totalSeats = seats });
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString()!;
    }

    private Task<HttpResponseMessage> Hold(string classId, string studentId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        req.Headers.Add("X-Student-Id", studentId);
        return _client.SendAsync(req);
    }

    private async Task<JsonElement> GetClass(string classId) =>
        await _client.GetFromJsonAsync<JsonElement>($"/classes/{classId}");

    [Fact]
    public async Task Hold_reduces_available_seats()
    {
        var classId = await CreateClass(10);

        var res = await Hold(classId, Sid(1));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var c = await GetClass(classId);
        Assert.Equal(1, c.GetProperty("heldSeats").GetInt32());
        Assert.Equal(9, c.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task Same_student_cannot_hold_same_class_twice()
    {
        var classId = await CreateClass(10);

        Assert.Equal(HttpStatusCode.Created, (await Hold(classId, Sid(2))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Hold(classId, Sid(2))).StatusCode);
    }

    [Fact]
    public async Task Same_student_cannot_apply_to_two_classes()
    {
        var a = await CreateClass(10);
        var b = await CreateClass(10);

        Assert.Equal(HttpStatusCode.Created, (await Hold(a, Sid(7))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Hold(b, Sid(7))).StatusCode);
    }

    [Fact]
    public async Task Confirm_twice_increments_confirmed_seats_once()
    {
        var classId = await CreateClass(10);
        var created = await (await Hold(classId, Sid(3))).Content.ReadFromJsonAsync<JsonElement>();
        var holdId = created.GetProperty("holdId").GetString();

        for (var i = 0; i < 2; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdId}/confirm");
            req.Headers.Add("X-Student-Id", Sid(3));
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(req)).StatusCode);
        }

        var c = await GetClass(classId);
        Assert.Equal(1, c.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, c.GetProperty("heldSeats").GetInt32());
    }

    [Fact]
    public async Task Concurrent_holds_never_exceed_total_seats()
    {
        const int seats = 5;
        const int attempts = 40;
        var classId = await CreateClass(seats);

        var results = await Task.WhenAll(
            Enumerable.Range(100, attempts).Select(n => Hold(classId, Sid(n))));

        var created = results.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflicts = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(seats, created);
        Assert.Equal(attempts - seats, conflicts);

        var c = await GetClass(classId);
        Assert.Equal(seats, c.GetProperty("heldSeats").GetInt32() + c.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, c.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task Two_backend_instances_never_exceed_total_seats()
    {
        const int seats = 5;
        const int attempts = 40;
        var classId = await CreateClass(seats);

        using var second = new ApiFactory();
        var client2 = second.CreateClient();

        Task<HttpResponseMessage> Send(HttpClient c, string studentId)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
            req.Headers.Add("X-Student-Id", studentId);
            return c.SendAsync(req);
        }

        var results = await Task.WhenAll(
            Enumerable.Range(200, attempts).Select(n => Send(n % 2 == 0 ? _client : client2, Sid(n))));

        Assert.Equal(seats, results.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(attempts - seats, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var c = await GetClass(classId);
        Assert.Equal(seats, c.GetProperty("heldSeats").GetInt32() + c.GetProperty("confirmedSeats").GetInt32());
    }
}