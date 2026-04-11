using System.ComponentModel.DataAnnotations;

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

    public Cow Cow { get; set; } = null!;
}
