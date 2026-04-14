using SmartCowFarm.Functions.Models;

namespace SmartCowFarm.Functions.Services;

public interface IAlertService
{
    /// <summary>Returns all unresolved alerts, newest first.</summary>
    Task<IEnumerable<Alert>> GetAlertsAsync();

    /// <summary>Marks an alert as resolved. Returns the updated alert, or null if not found.</summary>
    Task<Alert?> ResolveAlertAsync(Guid id);

    /// <summary>Persists a batch of new alerts.</summary>
    Task AddAlertsAsync(IEnumerable<Alert> alerts);
}
