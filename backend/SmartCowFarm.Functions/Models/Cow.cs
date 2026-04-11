using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCowFarm.Functions.Models;

public enum Gender { Male, Female }

public class Cow
{
    [Key]
    public Guid CowId { get; set; } = Guid.NewGuid();

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public DateOnly BirthDate { get; set; }

    public double BodyTemp { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTimeOffset? LastMilking { get; set; }

    public DateOnly? NextVaxDue { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [NotMapped]
    public int Age => (int)((DateTimeOffset.UtcNow - BirthDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).TotalDays / 365.25);

    public ICollection<VaccinationRecord> VaccinationRecords { get; set; } = [];
}
