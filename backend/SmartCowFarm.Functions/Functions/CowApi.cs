using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartCowFarm.Functions.Models;
using SmartCowFarm.Functions.Services;

namespace SmartCowFarm.Functions.Functions;

public class CowApi(ICowService cowService, IAlertService alertService, ILogger<CowApi> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    // ─── Cow CRUD ────────────────────────────────────────────────────────────

    [Function("GetCows")]
    public async Task<IActionResult> GetCows(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cows")] HttpRequest req)
    {
        var cows = await cowService.GetAllCowsAsync();
        return new OkObjectResult(cows);
    }

    [Function("GetCow")]
    public async Task<IActionResult> GetCow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cows/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var cow = await cowService.GetCowAsync(id);
        return cow is null ? new NotFoundResult() : new OkObjectResult(cow);
    }

    [Function("CreateCow")]
    public async Task<IActionResult> CreateCow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cows")] HttpRequest req)
    {
        var dto = await DeserializeAsync<CowDto>(req, "CreateCow");
        if (dto is null) return new BadRequestObjectResult("Invalid or missing JSON body");

        var cow = await cowService.CreateCowAsync(new CowPayload(
            dto.Gender, dto.BirthDate, dto.BodyTemp,
            dto.Latitude, dto.Longitude, dto.LastMilking, dto.NextVaxDue));

        return new ObjectResult(cow) { StatusCode = 201 };
    }

    [Function("UpdateCow")]
    public async Task<IActionResult> UpdateCow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cows/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var dto = await DeserializeAsync<CowDto>(req, "UpdateCow");
        if (dto is null) return new BadRequestObjectResult("Invalid or missing JSON body");

        var cow = await cowService.UpdateCowAsync(id, new CowPayload(
            dto.Gender, dto.BirthDate, dto.BodyTemp,
            dto.Latitude, dto.Longitude, dto.LastMilking, dto.NextVaxDue));

        return cow is null ? new NotFoundResult() : new OkObjectResult(cow);
    }

    [Function("DeleteCow")]
    public async Task<IActionResult> DeleteCow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cows/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var deleted = await cowService.DeleteCowAsync(id);
        return deleted ? new NoContentResult() : new NotFoundResult();
    }

    // ─── Vaccination Records ─────────────────────────────────────────────────

    [Function("GetVaccinations")]
    public async Task<IActionResult> GetVaccinations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cows/{cowId:guid}/vaccinations")] HttpRequest req,
        Guid cowId)
    {
        var records = await cowService.GetVaccinationsAsync(cowId);
        return records is null ? new NotFoundObjectResult("Cow not found") : new OkObjectResult(records);
    }

    [Function("AddVaccination")]
    public async Task<IActionResult> AddVaccination(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cows/{cowId:guid}/vaccinations")] HttpRequest req,
        Guid cowId)
    {
        var dto = await DeserializeAsync<VaccinationDto>(req, "AddVaccination");
        if (dto is null) return new BadRequestObjectResult("Invalid or missing JSON body");

        var record = await cowService.AddVaccinationAsync(cowId,
            new VaccinationPayload(dto.VaccineName, dto.AdministeredDate, dto.NextDueDate));

        return record is null
            ? new NotFoundObjectResult("Cow not found")
            : new ObjectResult(record) { StatusCode = 201 };
    }

    // ─── Alerts ──────────────────────────────────────────────────────────────

    [Function("GetAlerts")]
    public async Task<IActionResult> GetAlerts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alerts")] HttpRequest req)
    {
        var alerts = await alertService.GetAlertsAsync();
        return new OkObjectResult(alerts);
    }

    [Function("ResolveAlert")]
    public async Task<IActionResult> ResolveAlert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "alerts/{id:guid}/resolve")] HttpRequest req,
        Guid id)
    {
        var alert = await alertService.ResolveAlertAsync(id);
        return alert is null ? new NotFoundResult() : new OkObjectResult(alert);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<T?> DeserializeAsync<T>(HttpRequest req, string operation)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(req.Body, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize {Operation} request body", operation);
            return default;
        }
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
