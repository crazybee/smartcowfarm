using Microsoft.EntityFrameworkCore;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Models;
using SmartCowFarm.Functions.Services;
using Xunit;

namespace SmartCowFarm.Tests;

public class AlertServiceTests : IDisposable
{
    private readonly CowFarmDbContext _db;
    private readonly AlertService _sut;

    public AlertServiceTests()
    {
        var options = new DbContextOptionsBuilder<CowFarmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CowFarmDbContext(options);
        _sut = new AlertService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── GetAlertsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAlertsAsync_ReturnsOnlyUnresolved()
    {
        var cowId = Guid.NewGuid();
        _db.Alerts.AddRange(
            new Alert { CowId = cowId, AlertType = AlertType.HighTemperature, Message = "Hot", IsResolved = false },
            new Alert { CowId = cowId, AlertType = AlertType.GeofenceBreach, Message = "Outside", IsResolved = true }
        );
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAlertsAsync()).ToList();

        Assert.Single(result);
        Assert.False(result[0].IsResolved);
        Assert.Equal(AlertType.HighTemperature, result[0].AlertType);
    }

    [Fact]
    public async Task GetAlertsAsync_OrderedByCreatedAtDescending()
    {
        var cowId = Guid.NewGuid();
        var older = new Alert { CowId = cowId, AlertType = AlertType.HighTemperature, Message = "older", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) };
        var newer = new Alert { CowId = cowId, AlertType = AlertType.GeofenceBreach, Message = "newer", CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        _db.Alerts.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAlertsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("newer", result[0].Message); // newest first
        Assert.Equal("older", result[1].Message);
    }

    [Fact]
    public async Task GetAlertsAsync_EmptyDb_ReturnsEmpty()
    {
        var result = await _sut.GetAlertsAsync();
        Assert.Empty(result);
    }

    // ─── ResolveAlertAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAlertAsync_SetsIsResolvedTrue()
    {
        var alert = new Alert { CowId = Guid.NewGuid(), AlertType = AlertType.HighTemperature, Message = "Hot", IsResolved = false };
        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync();

        var result = await _sut.ResolveAlertAsync(alert.AlertId);

        Assert.NotNull(result);
        Assert.True(result!.IsResolved);
        Assert.True((await _db.Alerts.FindAsync(alert.AlertId))!.IsResolved);
    }

    [Fact]
    public async Task ResolveAlertAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.ResolveAlertAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // ─── AddAlertsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddAlertsAsync_PersistsAllAlerts()
    {
        var cowId = Guid.NewGuid();
        var alerts = new List<Alert>
        {
            new() { CowId = cowId, AlertType = AlertType.HighTemperature, Message = "Hot" },
            new() { CowId = cowId, AlertType = AlertType.GeofenceBreach, Message = "Outside" }
        };

        await _sut.AddAlertsAsync(alerts);

        Assert.Equal(2, await _db.Alerts.CountAsync());
    }

    [Fact]
    public async Task AddAlertsAsync_EmptyList_DoesNotThrow()
    {
        await _sut.AddAlertsAsync([]);
        Assert.Equal(0, await _db.Alerts.CountAsync());
    }
}
