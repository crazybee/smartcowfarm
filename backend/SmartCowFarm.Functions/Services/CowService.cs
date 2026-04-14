using Microsoft.EntityFrameworkCore;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Models;

namespace SmartCowFarm.Functions.Services;

public class CowService(CowFarmDbContext db, NotificationService notificationService) : ICowService
{
    // ─── Cows ─────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<CowSummary>> GetAllCowsAsync()
    {
        var cows = await db.Cows.AsNoTracking().ToListAsync();
        var cowIds = cows.Select(c => c.CowId).ToList();

        var latestAlerts = await db.Alerts
            .Where(a => cowIds.Contains(a.CowId) && !a.IsResolved)
            .GroupBy(a => a.CowId)
            .Select(g => g.OrderByDescending(a => a.CreatedAt).First())
            .ToListAsync();

        var alertMap = latestAlerts.ToDictionary(a => a.CowId);

        return cows.Select(c => new CowSummary(
            c.CowId,
            c.Gender,
            c.BirthDate,
            c.Age,
            c.BodyTemp,
            c.Latitude,
            c.Longitude,
            c.LastMilking,
            c.NextVaxDue,
            c.CreatedAt,
            c.UpdatedAt,
            alertMap.TryGetValue(c.CowId, out var alert)
                ? new AlertSummary(alert.AlertType, alert.Message, alert.CreatedAt)
                : null));
    }

    public async Task<CowDetail?> GetCowAsync(Guid id)
    {
        var cow = await db.Cows
            .AsNoTracking()
            .Include(c => c.VaccinationRecords)
            .FirstOrDefaultAsync(c => c.CowId == id);

        if (cow is null) return null;

        return new CowDetail(
            cow.CowId,
            cow.Gender,
            cow.BirthDate,
            cow.Age,
            cow.BodyTemp,
            cow.Latitude,
            cow.Longitude,
            cow.LastMilking,
            cow.NextVaxDue,
            cow.CreatedAt,
            cow.UpdatedAt,
            cow.VaccinationRecords.Select(v => new VaccinationSummary(
                v.RecordId,
                v.CowId,
                v.VaccineName,
                v.AdministeredDate,
                v.NextDueDate)));
    }

    public async Task<Cow> CreateCowAsync(CowPayload payload)
    {
        var cow = new Cow
        {
            Gender = payload.Gender,
            BirthDate = payload.BirthDate,
            BodyTemp = payload.BodyTemp,
            Latitude = payload.Latitude,
            Longitude = payload.Longitude,
            LastMilking = payload.LastMilking,
            NextVaxDue = payload.NextVaxDue
        };

        db.Cows.Add(cow);

        var newAlerts = notificationService.CheckTemperatureAlert(cow).ToList();
        if (newAlerts.Count > 0) db.Alerts.AddRange(newAlerts);

        await db.SaveChangesAsync();
        return cow;
    }

    public async Task<Cow?> UpdateCowAsync(Guid id, CowPayload payload)
    {
        var cow = await db.Cows.FindAsync(id);
        if (cow is null) return null;

        cow.Gender = payload.Gender;
        cow.BirthDate = payload.BirthDate;
        cow.BodyTemp = payload.BodyTemp;
        cow.Latitude = payload.Latitude;
        cow.Longitude = payload.Longitude;
        cow.LastMilking = payload.LastMilking;
        cow.NextVaxDue = payload.NextVaxDue;
        cow.UpdatedAt = DateTimeOffset.UtcNow;

        // Resolve any existing open temperature alerts for this cow, then re-evaluate
        var oldTempAlerts = await db.Alerts
            .Where(a => a.CowId == cow.CowId && !a.IsResolved &&
                        (a.AlertType == AlertType.HighTemperature || a.AlertType == AlertType.LowTemperature))
            .ToListAsync();
        foreach (var a in oldTempAlerts) a.IsResolved = true;

        var newAlerts = notificationService.CheckTemperatureAlert(cow).ToList();
        if (newAlerts.Count > 0) db.Alerts.AddRange(newAlerts);

        await db.SaveChangesAsync();
        return cow;
    }

    public async Task<bool> DeleteCowAsync(Guid id)
    {
        var cow = await db.Cows.FindAsync(id);
        if (cow is null) return false;

        db.Cows.Remove(cow);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Cow?> UpdateCowTelemetryAsync(Guid cowId, double temperature, double latitude, double longitude)
    {
        var cow = await db.Cows.FindAsync(cowId);
        if (cow is null) return null;

        cow.BodyTemp = temperature;
        cow.Latitude = latitude;
        cow.Longitude = longitude;
        cow.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return cow;
    }

    public async Task<IEnumerable<Cow>> GetCowsMissingTemperatureAlertsAsync()
    {
        // All cows with abnormal temps
        var abnormalCows = await db.Cows
            .Where(c => c.BodyTemp > 39.5 || c.BodyTemp < 38.0)
            .ToListAsync();

        if (abnormalCows.Count == 0) return [];

        // Of those, find the ones that already have an open temperature alert
        var cowIds = abnormalCows.Select(c => c.CowId).ToList();
        var coveredIds = await db.Alerts
            .Where(a => cowIds.Contains(a.CowId) && !a.IsResolved &&
                        (a.AlertType == AlertType.HighTemperature || a.AlertType == AlertType.LowTemperature))
            .Select(a => a.CowId)
            .Distinct()
            .ToListAsync();

        return abnormalCows.Where(c => !coveredIds.Contains(c.CowId));
    }

    public async Task<IEnumerable<Alert>> GetVaccinationDueAlertsAsync()
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var dueCows = await db.Cows
            .Where(c => c.NextVaxDue != null && c.NextVaxDue <= cutoff)
            .ToListAsync();

        if (dueCows.Count == 0) return [];

        // Skip cows that already have an open vaccination alert
        var dueCowIds = dueCows.Select(c => c.CowId).ToList();
        var coveredIds = await db.Alerts
            .Where(a => dueCowIds.Contains(a.CowId) && !a.IsResolved && a.AlertType == AlertType.VaccinationDue)
            .Select(a => a.CowId)
            .Distinct()
            .ToListAsync();

        return dueCows
            .Where(c => !coveredIds.Contains(c.CowId))
            .Select(c => new Alert
            {
                CowId = c.CowId,
                AlertType = AlertType.VaccinationDue,
                Message = $"Cow {c.CowId} vaccination due on {c.NextVaxDue}",
                IsResolved = false,
                CreatedAt = DateTimeOffset.UtcNow
            });
    }

    // ─── Vaccinations ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<VaccinationSummary>?> GetVaccinationsAsync(Guid cowId)
    {
        if (!await db.Cows.AnyAsync(c => c.CowId == cowId))
            return null;

        var records = await db.VaccinationRecords
            .AsNoTracking()
            .Where(v => v.CowId == cowId)
            .OrderByDescending(v => v.AdministeredDate)
            .ToListAsync();

        return records.Select(v => new VaccinationSummary(
            v.RecordId,
            v.CowId,
            v.VaccineName,
            v.AdministeredDate,
            v.NextDueDate));
    }

    public async Task<VaccinationRecord?> AddVaccinationAsync(Guid cowId, VaccinationPayload payload)
    {
        if (!await db.Cows.AnyAsync(c => c.CowId == cowId))
            return null;

        var record = new VaccinationRecord
        {
            CowId = cowId,
            VaccineName = payload.VaccineName,
            AdministeredDate = payload.AdministeredDate,
            NextDueDate = payload.NextDueDate
        };

        db.VaccinationRecords.Add(record);

        // Update cow's NextVaxDue if the new record sets an earlier due date
        if (payload.NextDueDate.HasValue)
        {
            var cow = await db.Cows.FindAsync(cowId);
            if (cow is not null && (cow.NextVaxDue is null || payload.NextDueDate.Value < cow.NextVaxDue.Value))
            {
                cow.NextVaxDue = payload.NextDueDate.Value;
                cow.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return record;
    }
}
