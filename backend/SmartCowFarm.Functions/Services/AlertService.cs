using Microsoft.EntityFrameworkCore;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Models;

namespace SmartCowFarm.Functions.Services;

public class AlertService(CowFarmDbContext db) : IAlertService
{
    public async Task<IEnumerable<Alert>> GetAlertsAsync()
    {
        return await db.Alerts
            .AsNoTracking()
            .Where(a => !a.IsResolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Alert?> ResolveAlertAsync(Guid id)
    {
        var alert = await db.Alerts.FindAsync(id);
        if (alert is null) return null;

        alert.IsResolved = true;
        await db.SaveChangesAsync();
        return alert;
    }

    public async Task AddAlertsAsync(IEnumerable<Alert> alerts)
    {
        db.Alerts.AddRange(alerts);
        await db.SaveChangesAsync();
    }
}
