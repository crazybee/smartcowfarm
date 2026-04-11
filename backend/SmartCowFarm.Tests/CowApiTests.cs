using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Functions;
using SmartCowFarm.Functions.Models;
using Xunit;

namespace SmartCowFarm.Tests;

public class CowApiTests : IDisposable
{
    private readonly CowFarmDbContext _db;
    private readonly CowApi _sut;

    public CowApiTests()
    {
        var options = new DbContextOptionsBuilder<CowFarmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CowFarmDbContext(options);
        _sut = new CowApi(_db, NullLogger<CowApi>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private static HttpRequest CreateHttpRequest() =>
        new DefaultHttpContext().Request;

    // ─── GET /api/cows ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetCows_EmptyDb_ReturnsEmptyList()
    {
        var result = await _sut.GetCows(CreateHttpRequest()) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetCows_WithCows_ReturnsList()
    {
        _db.Cows.AddRange(CreateCow(), CreateCow());
        await _db.SaveChangesAsync();

        var result = await _sut.GetCows(CreateHttpRequest()) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value);
        Assert.Equal(2, list.Count());
    }

    // ─── POST /api/cows ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCow_ValidBody_ReturnsCow()
    {
        var json = """
            {
                "gender": 1,
                "birthDate": "2021-05-10",
                "bodyTemp": 38.5,
                "latitude": 51.5,
                "longitude": 0.1
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.CreateCow(req);
        Assert.IsType<CreatedAtRouteResult>(result);
        Assert.Equal(1, await _db.Cows.CountAsync());
    }

    [Fact]
    public async Task CreateCow_EmptyBody_ReturnsBadRequest()
    {
        var req = CreateHttpRequestWithBody("null");
        var result = await _sut.CreateCow(req);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── GET /api/alerts ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAlerts_NoAlerts_ReturnsEmptyList()
    {
        var result = await _sut.GetAlerts(CreateHttpRequest()) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<Alert>>(result.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAlerts_OnlyReturnsUnresolved()
    {
        var cowId = Guid.NewGuid();
        _db.Alerts.AddRange(
            new Alert { CowId = cowId, AlertType = AlertType.HighTemperature, Message = "Hot", IsResolved = false },
            new Alert { CowId = cowId, AlertType = AlertType.HighTemperature, Message = "Hot resolved", IsResolved = true }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAlerts(CreateHttpRequest()) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<Alert>>(result.Value).ToList();
        Assert.Single(list);
        Assert.False(list[0].IsResolved);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Cow CreateCow() => new()
    {
        Gender = Gender.Female,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2))
    };

    private static HttpRequest CreateHttpRequestWithBody(string json)
    {
        var ctx = new DefaultHttpContext();
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentType = "application/json";
        return ctx.Request;
    }
}
