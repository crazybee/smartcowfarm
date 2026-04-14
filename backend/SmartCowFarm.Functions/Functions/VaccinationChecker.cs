using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartCowFarm.Functions.Models;
using SmartCowFarm.Functions.Services;

namespace SmartCowFarm.Functions.Functions;

public class VaccinationChecker(ICowService cowService, IAlertService alertService, NotificationService notificationService, ILogger<VaccinationChecker> logger)
{
    [Function("CheckVaccinationsDue")]
    [SignalROutput(HubName = "cowfarm")]
    public async Task<SignalRMessageAction?> Run([TimerTrigger("0 0 0 * * *", RunOnStartup = true)] TimerInfo timer)
    {
        logger.LogInformation("Scheduled alert check triggered at: {Time}", DateTimeOffset.UtcNow);

        var allAlerts = new List<Alert>();

        // ─ Vaccination due alerts ──────────────────────────────────────────────────────────────
        var vaccinationAlerts = await cowService.GetVaccinationDueAlertsAsync();
        allAlerts.AddRange(vaccinationAlerts);
        if (vaccinationAlerts.Any())
            logger.LogInformation("Created {Count} vaccination due alerts", vaccinationAlerts.Count());

        // ─ Temperature backfill (cows with no open alert but abnormal temp) ───────────────────
        var uncoveredCows = await cowService.GetCowsMissingTemperatureAlertsAsync();
        var tempAlerts = uncoveredCows.SelectMany(c => notificationService.CheckTemperatureAlert(c)).ToList();
        allAlerts.AddRange(tempAlerts);
        if (tempAlerts.Count > 0)
            logger.LogInformation("Backfilled {Count} temperature alerts", tempAlerts.Count);

        if (allAlerts.Count > 0)
        {
            await alertService.AddAlertsAsync(allAlerts);
            return new SignalRMessageAction("cowAlerts")
            {
                Arguments = [allAlerts]
            };
        }

        return null;
    }
}
