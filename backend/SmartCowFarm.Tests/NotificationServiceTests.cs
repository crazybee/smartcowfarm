using SmartCowFarm.Functions.Models;
using SmartCowFarm.Functions.Services;
using Xunit;

namespace SmartCowFarm.Tests;

public class NotificationServiceTests
{
    private readonly NotificationService _sut = new();

    // ─── Temperature Alerts ───────────────────────────────────────────────────

    [Fact]
    public void CheckTemperatureAlert_NormalTemp_ReturnsNoAlert()
    {
        var cow = CreateCow(bodyTemp: 38.5);
        var alerts = _sut.CheckTemperatureAlert(cow).ToList();
        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckTemperatureAlert_ExactlyAtThreshold_ReturnsNoAlert()
    {
        var cow = CreateCow(bodyTemp: 39.5);
        var alerts = _sut.CheckTemperatureAlert(cow).ToList();
        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckTemperatureAlert_HighTemp_ReturnsAlert()
    {
        var cow = CreateCow(bodyTemp: 40.2);
        var alerts = _sut.CheckTemperatureAlert(cow).ToList();
        Assert.Single(alerts);
        Assert.Equal(AlertType.HighTemperature, alerts[0].AlertType);
        Assert.Equal(cow.CowId, alerts[0].CowId);
        Assert.Contains("40.2", alerts[0].Message);
    }

    // ─── Geofence Alerts ──────────────────────────────────────────────────────

    // Farm polygon: a simple square 0,0 to 1,1 (lat/lng)
    private static readonly double[] FenceLats = [0.0, 0.0, 1.0, 1.0, 0.0];
    private static readonly double[] FenceLngs = [0.0, 1.0, 1.0, 0.0, 0.0];

    [Fact]
    public void CheckGeofenceAlert_InsidePolygon_ReturnsNoAlert()
    {
        var cow = CreateCow(lat: 0.5, lng: 0.5);
        var alerts = _sut.CheckGeofenceAlert(cow, FenceLats, FenceLngs).ToList();
        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckGeofenceAlert_OutsidePolygon_ReturnsAlert()
    {
        var cow = CreateCow(lat: 5.0, lng: 5.0);
        var alerts = _sut.CheckGeofenceAlert(cow, FenceLats, FenceLngs).ToList();
        Assert.Single(alerts);
        Assert.Equal(AlertType.GeofenceBreach, alerts[0].AlertType);
        Assert.Equal(cow.CowId, alerts[0].CowId);
    }

    [Fact]
    public void CheckGeofenceAlert_InsufficientCoordinates_ReturnsNoAlert()
    {
        var cow = CreateCow(lat: 5.0, lng: 5.0);
        var alerts = _sut.CheckGeofenceAlert(cow, [0.0, 0.0], [0.0, 1.0]).ToList();
        Assert.Empty(alerts);
    }

    // ─── Vaccination Due Alerts ───────────────────────────────────────────────

    [Fact]
    public void CheckVaccinationDue_NoNextVaxDue_ReturnsNoAlert()
    {
        var cow = CreateCow();
        cow.NextVaxDue = null;
        var alerts = _sut.CheckVaccinationDue(cow).ToList();
        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckVaccinationDue_DueInMoreThan3Days_ReturnsNoAlert()
    {
        var cow = CreateCow();
        cow.NextVaxDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var alerts = _sut.CheckVaccinationDue(cow).ToList();
        Assert.Empty(alerts);
    }

    [Fact]
    public void CheckVaccinationDue_DueIn2Days_ReturnsAlert()
    {
        var cow = CreateCow();
        cow.NextVaxDue = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var alerts = _sut.CheckVaccinationDue(cow).ToList();
        Assert.Single(alerts);
        Assert.Equal(AlertType.VaccinationDue, alerts[0].AlertType);
        Assert.Equal(cow.CowId, alerts[0].CowId);
    }

    [Fact]
    public void CheckVaccinationDue_DueToday_ReturnsAlert()
    {
        var cow = CreateCow();
        cow.NextVaxDue = DateOnly.FromDateTime(DateTime.UtcNow);
        var alerts = _sut.CheckVaccinationDue(cow).ToList();
        Assert.Single(alerts);
        Assert.Equal(AlertType.VaccinationDue, alerts[0].AlertType);
    }

    private static Cow CreateCow(double bodyTemp = 38.5, double lat = 0.5, double lng = 0.5) =>
        new()
        {
            CowId = Guid.NewGuid(),
            Gender = Gender.Female,
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
            BodyTemp = bodyTemp,
            Latitude = lat,
            Longitude = lng
        };
}
