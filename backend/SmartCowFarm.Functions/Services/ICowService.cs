using SmartCowFarm.Functions.Models;

namespace SmartCowFarm.Functions.Services;

public interface ICowService
{
    // ─── Cows ─────────────────────────────────────────────────────────────────

    /// <summary>Returns all cows with each cow's latest unresolved alert.</summary>
    Task<IEnumerable<CowSummary>> GetAllCowsAsync();

    /// <summary>Returns a single cow with its vaccination records, or null if not found.</summary>
    Task<CowDetail?> GetCowAsync(Guid id);

    /// <summary>Creates and persists a new cow. Returns the saved entity.</summary>
    Task<Cow> CreateCowAsync(CowPayload payload);

    /// <summary>Updates an existing cow. Returns the updated entity, or null if not found.</summary>
    Task<Cow?> UpdateCowAsync(Guid id, CowPayload payload);

    /// <summary>Deletes a cow. Returns false if not found.</summary>
    Task<bool> DeleteCowAsync(Guid id);

    /// <summary>Updates a cow's live telemetry fields (temperature, location) and persists. Returns the updated entity, or null if not found.</summary>
    Task<Cow?> UpdateCowTelemetryAsync(Guid cowId, double temperature, double latitude, double longitude);

    /// <summary>Returns all cows that currently have an abnormal temperature and no open temperature alert.</summary>
    Task<IEnumerable<Cow>> GetCowsMissingTemperatureAlertsAsync();

    /// <summary>Returns vaccination-due alerts for cows whose next vax date is within 3 days, skipping cows that already have an open vaccination alert.</summary>
    Task<IEnumerable<Alert>> GetVaccinationDueAlertsAsync();

    // ─── Vaccinations ─────────────────────────────────────────────────────────

    /// <summary>Returns all vaccination records for a cow, newest first. Returns null if the cow doesn't exist.</summary>
    Task<IEnumerable<VaccinationSummary>?> GetVaccinationsAsync(Guid cowId);

    /// <summary>Adds a vaccination record and updates the cow's NextVaxDue if appropriate. Returns null if cow not found.</summary>
    Task<VaccinationRecord?> AddVaccinationAsync(Guid cowId, VaccinationPayload payload);
}

// ─── Shared result / payload shapes ──────────────────────────────────────────

public record CowPayload(
    Gender Gender,
    DateOnly BirthDate,
    double BodyTemp,
    double Latitude,
    double Longitude,
    DateTimeOffset? LastMilking,
    DateOnly? NextVaxDue);

public record VaccinationPayload(
    string VaccineName,
    DateOnly AdministeredDate,
    DateOnly? NextDueDate);

public record AlertSummary(AlertType AlertType, string Message, DateTimeOffset CreatedAt);

public record CowSummary(
    Guid CowId,
    Gender Gender,
    DateOnly BirthDate,
    int Age,
    double BodyTemp,
    double Latitude,
    double Longitude,
    DateTimeOffset? LastMilking,
    DateOnly? NextVaxDue,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    AlertSummary? LatestAlert);

public record VaccinationSummary(
    Guid RecordId,
    Guid CowId,
    string VaccineName,
    DateOnly AdministeredDate,
    DateOnly? NextDueDate);

public record CowDetail(
    Guid CowId,
    Gender Gender,
    DateOnly BirthDate,
    int Age,
    double BodyTemp,
    double Latitude,
    double Longitude,
    DateTimeOffset? LastMilking,
    DateOnly? NextVaxDue,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IEnumerable<VaccinationSummary> VaccinationRecords);
