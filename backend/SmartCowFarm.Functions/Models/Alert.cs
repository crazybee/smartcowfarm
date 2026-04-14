using System.ComponentModel.DataAnnotations;

namespace SmartCowFarm.Functions.Models;

public enum AlertType { HighTemperature, GeofenceBreach, VaccinationDue, LowTemperature }

public class Alert
{
    [Key]
    public Guid AlertId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CowId { get; set; }

    [Required]
    public AlertType AlertType { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
