using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Models;

namespace SmartCowFarm.Functions.Functions;

public class CowApi(CowFarmDbContext db, ILogger<CowApi> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // ─── Cow CRUD ────────────────────────────────────────────────────────────

    [Function("GetCows")]
    public async Task<IActionResult> GetCows(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cows")] HttpRequest req)
    {
        var cows = await db.Cows.AsNoTracking().ToListAsync();
        var cowIds = cows.Select(c => c.CowId).ToList();

        var latestAlerts = await db.Alerts
            .Where(a => cowIds.Contains(a.CowId) && !a.IsResolved)
            .GroupBy(a => a.CowId)
            .Select(g => g.OrderByDescending(a => a.CreatedAt).First())
            .ToListAsync();

        var alertMap = latestAlerts.ToDictionary(a => a.CowId);

        var result = cows.Select(c => new
        {
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
            LatestAlert = alertMap.TryGetValue(c.CowId, out var alert) ? new { alert.AlertType, alert.Message, alert.CreatedAt } : null
        });

        return new OkObjectResult(result);
    }

    [Function("GetCow")]
    public async Task<IActionResult> GetCow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cows/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var cow = await db.Cows
            .AsNoTracking()
            .Include(c => c.VaccinationRecords)
            .FirstOrDefaultAsync(c => c.CowId == id);

        if (cow is null) return new NotFoundResult();

        return new OkObjectResult(new
        {
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
            VaccinationRecords = cow.VaccinationRecords.Select(v => new
            {
                v.RecordId,
                v.VaccineName,
                v.AdministeredDate,
                v.NextDueDate
            })
        });
    }

    [Function("CreateCow")]
    public async Task<IActionResult> CreateCow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cows")] HttpRequest req)
    {
        CowDto? dto;
        try
        {
            dto = await JsonSerializer.DeserializeAsync<CowDto>(req.Body, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize CreateCow request body");
            return new BadRequestObjectResult("Invalid JSON body");
        }

        if (dto is null) return new BadRequestObjectResult("Request body is required");

        var cow = new Cow
        {
            Gender = dto.Gender,
            BirthDate = dto.BirthDate,
            BodyTemp = dto.BodyTemp,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            LastMilking = dto.LastMilking,
            NextVaxDue = dto.NextVaxDue
        };

        db.Cows.Add(cow);
        await db.SaveChangesAsync();
        return new CreatedAtRouteResult(null, new { cow.CowId }, cow);
    }

    [Function("UpdateCow")]
    public async Task<IActionResult> UpdateCow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cows/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var cow = await db.Cows.FindAsync(id);
        if (cow is null) return new NotFoundResult();

        CowDto? dto;
        try
        {
            dto = await JsonSerializer.DeserializeAsync<CowDto>(req.Body, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize UpdateCow request body for cow {Id}", id);
            return new BadRequestObjectResult("Invalid JSON body");
        }

        if (dto is null) return new BadRequestObjectResult("Request body is required");

        cow.Gender = dto.Gender;
        cow.BirthDate = dto.BirthDate;
        cow.BodyTemp = dto.BodyTemp;
        cow.Latitude = dto.Latitude;
        cow.Longitude = dto.Longitude;
        cow.LastMilking = dto.LastMilking;
        cow.NextVaxDue = dto.NextVaxDue;
        cow.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return new OkObjectResult(cow);
    }

    [Function("DeleteCow")]
    public async Task<IActionResult> DeleteCow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cows/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var cow = await db.Cows.FindAsync(id);
        if (cow is null) return new NotFoundResult();

        db.Cows.Remove(cow);
        await db.SaveChangesAsync();
        return new NoContentResult();
    }

    // ─── Vaccination Records ─────────────────────────────────────────────────

    [Function("GetVaccinations")]
    public async Task<IActionResult> GetVaccinations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cows/{cowId:guid}/vaccinations")] HttpRequest req,
        Guid cowId)
    {
        if (!await db.Cows.AnyAsync(c => c.CowId == cowId))
            return new NotFoundObjectResult("Cow not found");

        var records = await db.VaccinationRecords
            .AsNoTracking()
            .Where(v => v.CowId == cowId)
            .OrderByDescending(v => v.AdministeredDate)
            .ToListAsync();

        return new OkObjectResult(records.Select(v => new
        {
            v.RecordId,
            v.CowId,
            v.VaccineName,
            v.AdministeredDate,
            v.NextDueDate
        }));
    }

    [Function("AddVaccination")]
    public async Task<IActionResult> AddVaccination(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cows/{cowId:guid}/vaccinations")] HttpRequest req,
        Guid cowId)
    {
        if (!await db.Cows.AnyAsync(c => c.CowId == cowId))
            return new NotFoundObjectResult("Cow not found");

        VaccinationDto? dto;
        try
        {
            dto = await JsonSerializer.DeserializeAsync<VaccinationDto>(req.Body, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize AddVaccination request body for cow {CowId}", cowId);
            return new BadRequestObjectResult("Invalid JSON body");
        }

        if (dto is null) return new BadRequestObjectResult("Request body is required");

        var record = new VaccinationRecord
        {
            CowId = cowId,
            VaccineName = dto.VaccineName,
            AdministeredDate = dto.AdministeredDate,
            NextDueDate = dto.NextDueDate
        };

        db.VaccinationRecords.Add(record);

        // Update cow's NextVaxDue if the new record sets an earlier due date
        if (dto.NextDueDate.HasValue)
        {
            var cow = await db.Cows.FindAsync(cowId);
            if (cow is not null && (cow.NextVaxDue is null || dto.NextDueDate.Value < cow.NextVaxDue.Value))
            {
                cow.NextVaxDue = dto.NextDueDate.Value;
                cow.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return new CreatedAtRouteResult(null, new { record.RecordId }, record);
    }

    // ─── Alerts ──────────────────────────────────────────────────────────────

    [Function("GetAlerts")]
    public async Task<IActionResult> GetAlerts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alerts")] HttpRequest req)
    {
        var alerts = await db.Alerts
            .AsNoTracking()
            .Where(a => !a.IsResolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return new OkObjectResult(alerts);
    }

    [Function("ResolveAlert")]
    public async Task<IActionResult> ResolveAlert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "alerts/{id:guid}/resolve")] HttpRequest req,
        Guid id)
    {
        var alert = await db.Alerts.FindAsync(id);
        if (alert is null) return new NotFoundResult();

        alert.IsResolved = true;
        await db.SaveChangesAsync();
        return new OkObjectResult(alert);
    }

    // ─── DTOs ────────────────────────────────────────────────────────────────

    private record CowDto(
        Gender Gender,
        DateOnly BirthDate,
        double BodyTemp,
        double Latitude,
        double Longitude,
        DateTimeOffset? LastMilking,
        DateOnly? NextVaxDue);

    private record VaccinationDto(
        string VaccineName,
        DateOnly AdministeredDate,
        DateOnly? NextDueDate);
}
