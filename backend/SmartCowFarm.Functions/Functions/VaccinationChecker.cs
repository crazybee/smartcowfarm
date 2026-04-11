using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Models;
using SmartCowFarm.Functions.Services;

namespace SmartCowFarm.Functions.Functions;

public class VaccinationChecker(CowFarmDbContext db, NotificationService notificationService, ILogger<VaccinationChecker> logger)
{
    [Function("CheckVaccinationsDue")]
    [SignalROutput(HubName = "cowfarm")]
    public async Task<SignalRMessageAction?> Run([TimerTrigger("0 0 0 * * *")] TimerInfo timer)
    {
        logger.LogInformation("Vaccination check triggered at: {Time}", DateTimeOffset.UtcNow);

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var cows = await db.Cows
            .Where(c => c.NextVaxDue != null && c.NextVaxDue <= cutoff)
            .ToListAsync();

        var alerts = cows
            .SelectMany(cow => notificationService.CheckVaccinationDue(cow))
            .ToList();

        if (alerts.Count > 0)
        {
            db.Alerts.AddRange(alerts);
            await db.SaveChangesAsync();
            logger.LogInformation("Created {Count} vaccination due alerts", alerts.Count);

            return new SignalRMessageAction("cowAlerts")
            {
                Arguments = [alerts]
            };
        }

        return null;
    }
}
