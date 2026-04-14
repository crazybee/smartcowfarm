using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartCowFarm.Functions.Models;
using SmartCowFarm.Functions.Services;

namespace SmartCowFarm.Functions.Functions;

public class IoTHubProcessor(ICowService cowService, IAlertService alertService, NotificationService notificationService, ILogger<IoTHubProcessor> logger)
{
    [Function("ProcessTelemetry")]
    [SignalROutput(HubName = "cowfarm")]
    public async Task<SignalRMessageAction?> Run(
        [EventHubTrigger("telemetry", Connection = "IoTHubConnectionString", IsBatched = false)] string eventData)
    {
        TelemetryPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TelemetryPayload>(eventData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize telemetry payload");
            return null;
        }

        if (payload is null || !Guid.TryParse(payload.DeviceId, out var cowId))
        {
            logger.LogWarning("Invalid telemetry payload or DeviceId: {DeviceId}", payload?.DeviceId);
            return null;
        }

        var cow = await cowService.UpdateCowTelemetryAsync(cowId, payload.Temperature, payload.Latitude, payload.Longitude);
        if (cow is null)
        {
            logger.LogWarning("Cow not found for DeviceId: {DeviceId}", payload.DeviceId);
            return null;
        }

        var geofenceCoords = ParseGeofenceCoordinates();
        var alerts = new List<Alert>();
        alerts.AddRange(notificationService.CheckTemperatureAlert(cow));
        if (geofenceCoords.Latitudes.Length > 0)
            alerts.AddRange(notificationService.CheckGeofenceAlert(cow, geofenceCoords.Latitudes, geofenceCoords.Longitudes));

        if (alerts.Count > 0)
            await alertService.AddAlertsAsync(alerts);

        if (alerts.Count > 0)
        {
            return new SignalRMessageAction("cowAlerts")
            {
                Arguments = [alerts]
            };
        }

        return null;
    }

    private static (double[] Latitudes, double[] Longitudes) ParseGeofenceCoordinates()
    {
        var json = Environment.GetEnvironmentVariable("GeofenceCoordinates");
        if (string.IsNullOrEmpty(json)) return ([], []);

        try
        {
            var coords = JsonSerializer.Deserialize<double[][]>(json);
            if (coords is null) return ([], []);
            var lats = coords.Select(c => c[0]).ToArray();
            var lngs = coords.Select(c => c[1]).ToArray();
            return (lats, lngs);
        }
        catch
        {
            // Silently return empty coordinates; misconfigured GeofenceCoordinates env var
            // will result in geofence checks being skipped rather than crashing the function.
            return ([], []);
        }
    }
}
