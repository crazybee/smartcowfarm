using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartCowFarm.Functions.Models;

public class VaccinationRecord
{
    [Key]
    public Guid RecordId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CowId { get; set; }

    [Required]
    [MaxLength(200)]
    public string VaccineName { get; set; } = string.Empty;

    [Required]
    public DateOnly AdministeredDate { get; set; }

    public DateOnly? NextDueDate { get; set; }

    [JsonIgnore]
    public Cow Cow { get; set; } = null!;
}
