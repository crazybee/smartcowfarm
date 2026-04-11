using NetTopologySuite.Geometries;
using SmartCowFarm.Functions.Models;

namespace SmartCowFarm.Functions.Services;

public class NotificationService
{
    private static readonly GeometryFactory GeomFactory = new(new PrecisionModel(), 4326);

    public IEnumerable<Alert> CheckTemperatureAlert(Cow cow)
    {
        if (cow.BodyTemp > 39.5)
        {
            yield return new Alert
            {
                CowId = cow.CowId,
                AlertType = AlertType.HighTemperature,
                Message = $"Cow {cow.CowId} has high body temperature: {cow.BodyTemp:F1}°C (threshold: 39.5°C)",
                IsResolved = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public IEnumerable<Alert> CheckGeofenceAlert(Cow cow, double[] geofenceLatitudes, double[] geofenceLongitudes)
    {
        if (geofenceLatitudes.Length < 3 || geofenceLatitudes.Length != geofenceLongitudes.Length)
            yield break;

        var coordinates = geofenceLatitudes
            .Zip(geofenceLongitudes, (lat, lng) => new Coordinate(lng, lat))
            .ToList();

        // Close the polygon if not already closed
        if (!coordinates[0].Equals2D(coordinates[^1]))
            coordinates.Add(coordinates[0]);

        var polygon = GeomFactory.CreatePolygon([.. coordinates]);
        var cowPoint = GeomFactory.CreatePoint(new Coordinate(cow.Longitude, cow.Latitude));

        if (!polygon.Contains(cowPoint))
        {
            yield return new Alert
            {
                CowId = cow.CowId,
                AlertType = AlertType.GeofenceBreach,
                Message = $"Cow {cow.CowId} is outside farm boundary at ({cow.Latitude:F6}, {cow.Longitude:F6})",
                IsResolved = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public IEnumerable<Alert> CheckVaccinationDue(Cow cow)
    {
        if (cow.NextVaxDue is not null && cow.NextVaxDue <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)))
        {
            yield return new Alert
            {
                CowId = cow.CowId,
                AlertType = AlertType.VaccinationDue,
                Message = $"Cow {cow.CowId} vaccination is due on {cow.NextVaxDue:yyyy-MM-dd}",
                IsResolved = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
