using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Models;
using SmartCowFarm.Functions.Services;
using Xunit;

namespace SmartCowFarm.Tests;

public class CowServiceTests : IDisposable
{
    private readonly CowFarmDbContext _db;
    private readonly CowService _sut;

    public CowServiceTests()
    {
        var options = new DbContextOptionsBuilder<CowFarmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CowFarmDbContext(options);
        _sut = new CowService(_db, new NotificationService());
    }

    public void Dispose() => _db.Dispose();

    // ─── GetAllCowsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCowsAsync_EmptyDb_ReturnsEmpty()
    {
        var result = await _sut.GetAllCowsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllCowsAsync_WithCows_ReturnsSummaryForEach()
    {
        _db.Cows.AddRange(CreateCow(), CreateCow());
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllCowsAsync();
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllCowsAsync_IncludesLatestUnresolvedAlert()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        var older = new Alert { CowId = cow.CowId, AlertType = AlertType.HighTemperature, Message = "older", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) };
        var newer = new Alert { CowId = cow.CowId, AlertType = AlertType.GeofenceBreach, Message = "newer", CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        _db.Alerts.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAllCowsAsync()).Single();
        Assert.NotNull(result.LatestAlert);
        Assert.Equal(AlertType.GeofenceBreach, result.LatestAlert!.AlertType);
    }

    [Fact]
    public async Task GetAllCowsAsync_ExcludesResolvedAlerts()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        _db.Alerts.Add(new Alert { CowId = cow.CowId, AlertType = AlertType.HighTemperature, Message = "resolved", IsResolved = true });
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAllCowsAsync()).Single();
        Assert.Null(result.LatestAlert);
    }

    // ─── GetCowAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCowAsync_Found_ReturnsDetailWithVaccinations()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        _db.VaccinationRecords.Add(new VaccinationRecord
        {
            CowId = cow.CowId,
            VaccineName = "FMD",
            AdministeredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3))
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetCowAsync(cow.CowId);
        Assert.NotNull(result);
        Assert.Equal(cow.CowId, result!.CowId);
        Assert.Single(result.VaccinationRecords);
        Assert.Equal("FMD", result.VaccinationRecords.First().VaccineName);
    }

    [Fact]
    public async Task GetCowAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetCowAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // ─── CreateCowAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCowAsync_PersistsAndReturnsCow()
    {
        var payload = new CowPayload(Gender.Female, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)), 38.5, 51.5, 0.1, null, null);

        var result = await _sut.CreateCowAsync(payload);

        Assert.NotEqual(Guid.Empty, result.CowId);
        Assert.Equal(Gender.Female, result.Gender);
        Assert.Equal(1, await _db.Cows.CountAsync());
    }

    // ─── UpdateCowAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCowAsync_UpdatesAllFields()
    {
        var cow = CreateCow(gender: Gender.Female, bodyTemp: 38.0);
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var payload = new CowPayload(Gender.Male, cow.BirthDate, 39.0, 52.0, 1.0, null, null);
        var result = await _sut.UpdateCowAsync(cow.CowId, payload);

        Assert.NotNull(result);
        Assert.Equal(Gender.Male, result!.Gender);
        Assert.Equal(39.0, result.BodyTemp);
        Assert.Equal(52.0, result.Latitude);
    }

    [Fact]
    public async Task UpdateCowAsync_NotFound_ReturnsNull()
    {
        var payload = new CowPayload(Gender.Female, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)), 38.5, 0, 0, null, null);
        var result = await _sut.UpdateCowAsync(Guid.NewGuid(), payload);
        Assert.Null(result);
    }

    // ─── DeleteCowAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCowAsync_ExistingCow_DeletesAndReturnsTrue()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteCowAsync(cow.CowId);

        Assert.True(result);
        Assert.Equal(0, await _db.Cows.CountAsync());
    }

    [Fact]
    public async Task DeleteCowAsync_NotFound_ReturnsFalse()
    {
        var result = await _sut.DeleteCowAsync(Guid.NewGuid());
        Assert.False(result);
    }

    // ─── UpdateCowTelemetryAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateCowTelemetryAsync_UpdatesTelemetryFields()
    {
        var cow = CreateCow(bodyTemp: 38.0, lat: 0.0, lng: 0.0);
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateCowTelemetryAsync(cow.CowId, 40.1, 51.5, -0.5);

        Assert.NotNull(result);
        Assert.Equal(40.1, result!.BodyTemp);
        Assert.Equal(51.5, result.Latitude);
        Assert.Equal(-0.5, result.Longitude);
    }

    [Fact]
    public async Task UpdateCowTelemetryAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.UpdateCowTelemetryAsync(Guid.NewGuid(), 38.0, 0, 0);
        Assert.Null(result);
    }

    // ─── GetVaccinationsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetVaccinationsAsync_ReturnsRecordsNewestFirst()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        _db.VaccinationRecords.AddRange(
            new VaccinationRecord { CowId = cow.CowId, VaccineName = "A", AdministeredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)) },
            new VaccinationRecord { CowId = cow.CowId, VaccineName = "B", AdministeredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)) }
        );
        await _db.SaveChangesAsync();

        var result = (await _sut.GetVaccinationsAsync(cow.CowId))!.ToList();
        Assert.Equal(2, result.Count);
        Assert.Equal("B", result[0].VaccineName); // newest first
    }

    [Fact]
    public async Task GetVaccinationsAsync_CowNotFound_ReturnsNull()
    {
        var result = await _sut.GetVaccinationsAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // ─── AddVaccinationAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task AddVaccinationAsync_AddsRecordAndReturnsIt()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var payload = new VaccinationPayload("FMD", DateOnly.FromDateTime(DateTime.UtcNow), null);
        var result = await _sut.AddVaccinationAsync(cow.CowId, payload);

        Assert.NotNull(result);
        Assert.Equal("FMD", result!.VaccineName);
        Assert.Equal(1, await _db.VaccinationRecords.CountAsync());
    }

    [Fact]
    public async Task AddVaccinationAsync_UpdatesNextVaxDue_WhenNewDateIsEarlier()
    {
        var laterDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));
        var earlierDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var cow = CreateCow();
        cow.NextVaxDue = laterDue;
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        await _sut.AddVaccinationAsync(cow.CowId, new VaccinationPayload("Rabies", DateOnly.FromDateTime(DateTime.UtcNow), earlierDue));

        var updated = await _db.Cows.FindAsync(cow.CowId);
        Assert.Equal(earlierDue, updated!.NextVaxDue);
    }

    [Fact]
    public async Task AddVaccinationAsync_DoesNotUpdateNextVaxDue_WhenNewDateIsLater()
    {
        var existingDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var laterDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));
        var cow = CreateCow();
        cow.NextVaxDue = existingDue;
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        await _sut.AddVaccinationAsync(cow.CowId, new VaccinationPayload("Anthrax", DateOnly.FromDateTime(DateTime.UtcNow), laterDue));

        var notUpdated = await _db.Cows.FindAsync(cow.CowId);
        Assert.Equal(existingDue, notUpdated!.NextVaxDue);
    }

    [Fact]
    public async Task AddVaccinationAsync_CowNotFound_ReturnsNull()
    {
        var result = await _sut.AddVaccinationAsync(Guid.NewGuid(), new VaccinationPayload("FMD", DateOnly.FromDateTime(DateTime.UtcNow), null));
        Assert.Null(result);
    }

    // ─── GetCowsMissingTemperatureAlertsAsync ────────────────────────────────

    [Fact]
    public async Task GetCowsMissingTemperatureAlertsAsync_NormalTemp_NotReturned()
    {
        _db.Cows.Add(CreateCow(bodyTemp: 39.0)); // normal
        await _db.SaveChangesAsync();

        var result = await _sut.GetCowsMissingTemperatureAlertsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCowsMissingTemperatureAlertsAsync_HighTempNoAlert_IsReturned()
    {
        var cow = CreateCow(bodyTemp: 41.0);
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.GetCowsMissingTemperatureAlertsAsync();
        Assert.Single(result);
        Assert.Equal(cow.CowId, result.First().CowId);
    }

    [Fact]
    public async Task GetCowsMissingTemperatureAlertsAsync_LowTempNoAlert_IsReturned()
    {
        var cow = CreateCow(bodyTemp: 37.5);
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.GetCowsMissingTemperatureAlertsAsync();
        Assert.Single(result);
        Assert.Equal(cow.CowId, result.First().CowId);
    }

    [Fact]
    public async Task GetCowsMissingTemperatureAlertsAsync_HighTempWithOpenAlert_IsExcluded()
    {
        var cow = CreateCow(bodyTemp: 41.0);
        _db.Cows.Add(cow);
        _db.Alerts.Add(new Alert { CowId = cow.CowId, AlertType = AlertType.HighTemperature, Message = "hot", IsResolved = false, CreatedAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _sut.GetCowsMissingTemperatureAlertsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCowsMissingTemperatureAlertsAsync_HighTempWithResolvedAlertOnly_IsReturned()
    {
        var cow = CreateCow(bodyTemp: 41.0);
        _db.Cows.Add(cow);
        _db.Alerts.Add(new Alert { CowId = cow.CowId, AlertType = AlertType.HighTemperature, Message = "old", IsResolved = true, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) });
        await _db.SaveChangesAsync();

        var result = await _sut.GetCowsMissingTemperatureAlertsAsync();
        Assert.Single(result); // resolved alert doesn't count as coverage
    }

    // ─── GetVaccinationDueAlertsAsync ────────────────────────────────────────

    [Fact]
    public async Task GetVaccinationDueAlertsAsync_DueWithin3Days_AlertCreated()
    {
        var cow = CreateCow();
        cow.NextVaxDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.GetVaccinationDueAlertsAsync();
        Assert.Single(result);
        Assert.Equal(cow.CowId, result.First().CowId);
        Assert.Equal(AlertType.VaccinationDue, result.First().AlertType);
    }

    [Fact]
    public async Task GetVaccinationDueAlertsAsync_AlreadyHasOpenVaccinationAlert_IsSkipped()
    {
        var cow = CreateCow();
        cow.NextVaxDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        _db.Cows.Add(cow);
        _db.Alerts.Add(new Alert { CowId = cow.CowId, AlertType = AlertType.VaccinationDue, Message = "already alerted", IsResolved = false, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) });
        await _db.SaveChangesAsync();

        var result = await _sut.GetVaccinationDueAlertsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetVaccinationDueAlertsAsync_DueMoreThan3DaysAway_NotReturned()
    {
        var cow = CreateCow();
        cow.NextVaxDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.GetVaccinationDueAlertsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetVaccinationDueAlertsAsync_NullNextVaxDue_NotReturned()
    {
        var cow = CreateCow();
        cow.NextVaxDue = null;
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.GetVaccinationDueAlertsAsync();
        Assert.Empty(result);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Cow CreateCow(Gender gender = Gender.Female, double bodyTemp = 38.5, double lat = 51.5, double lng = 0.1) => new()
    {
        CowId = Guid.NewGuid(),
        Gender = gender,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
        BodyTemp = bodyTemp,
        Latitude = lat,
        Longitude = lng
    };
}
