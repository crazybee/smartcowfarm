namespace SmartCowFarm.Functions.Models;

public class TelemetryPayload
{
    public string DeviceId { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
