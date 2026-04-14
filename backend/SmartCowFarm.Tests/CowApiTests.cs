using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Functions;
using SmartCowFarm.Functions.Models;
using SmartCowFarm.Functions.Services;
using Xunit;

namespace SmartCowFarm.Tests;

public class CowApiTests : IDisposable
{
    private readonly CowFarmDbContext _db;
    private readonly CowApi _sut;

    public CowApiTests()
    {
        var options = new DbContextOptionsBuilder<CowFarmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CowFarmDbContext(options);
        var cowService = new CowService(_db, new NotificationService());
        var alertService = new AlertService(_db);
        _sut = new CowApi(cowService, alertService, NullLogger<CowApi>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private static HttpRequest CreateHttpRequest() =>
        new DefaultHttpContext().Request;

    // ─── GET /api/cows ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetCows_EmptyDb_ReturnsEmptyList()
    {
        var result = await _sut.GetCows(CreateHttpRequest()) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetCows_WithCows_ReturnsList()
    {
        _db.Cows.AddRange(CreateCow(), CreateCow());
        await _db.SaveChangesAsync();

        var result = await _sut.GetCows(CreateHttpRequest()) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value);
        Assert.Equal(2, list.Count());
    }

    // ─── POST /api/cows ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCow_ValidBody_IntegerGender_ReturnsCow()
    {
        var json = """
            {
                "gender": 1,
                "birthDate": "2021-05-10",
                "bodyTemp": 38.5,
                "latitude": 51.5,
                "longitude": 0.1
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.CreateCow(req);
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        Assert.Equal(1, await _db.Cows.CountAsync());
    }

    [Fact]
    public async Task CreateCow_GenderAsLowercaseString_ReturnsCow()
    {
        var json = """
            {
                "gender": "female",
                "birthDate": "2021-05-10",
                "bodyTemp": 38.5,
                "latitude": 51.5,
                "longitude": 0.1
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.CreateCow(req);
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        var cow = await _db.Cows.FirstAsync();
        Assert.Equal(Gender.Female, cow.Gender);
    }

    [Fact]
    public async Task CreateCow_GenderAsPascalCaseString_ReturnsCow()
    {
        var json = """
            {
                "gender": "Female",
                "birthDate": "2022-03-01",
                "bodyTemp": 38.2,
                "latitude": 52.0,
                "longitude": 0.5
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.CreateCow(req);
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        var cow = await _db.Cows.FirstAsync();
        Assert.Equal(Gender.Female, cow.Gender);
    }

    [Fact]
    public async Task CreateCow_GenderMale_ReturnsCowWithMaleGender()
    {
        var json = """
            {
                "gender": "male",
                "birthDate": "2020-06-15",
                "bodyTemp": 38.0,
                "latitude": 51.0,
                "longitude": 0.2
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.CreateCow(req);
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        var cow = await _db.Cows.FirstAsync();
        Assert.Equal(Gender.Male, cow.Gender);
    }

    [Fact]
    public async Task CreateCow_PropertyNamesCaseInsensitive_ReturnsCow()
    {
        // All property names in PascalCase (instead of camelCase)
        var json = """
            {
                "Gender": "Female",
                "BirthDate": "2021-05-10",
                "BodyTemp": 38.5,
                "Latitude": 51.5,
                "Longitude": 0.1
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.CreateCow(req);
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        Assert.Equal(1, await _db.Cows.CountAsync());
    }

    [Fact]
    public async Task CreateCow_InvalidGenderString_ReturnsBadRequest()
    {
        var json = """
            {
                "gender": "invalid",
                "birthDate": "2021-05-10",
                "bodyTemp": 38.5,
                "latitude": 51.5,
                "longitude": 0.1
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.CreateCow(req);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateCow_EmptyBody_ReturnsBadRequest()
    {
        var req = CreateHttpRequestWithBody("null");
        var result = await _sut.CreateCow(req);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateCow_InvalidJson_ReturnsBadRequest()
    {
        var req = CreateHttpRequestWithBody("{ this is not json }");
        var result = await _sut.CreateCow(req);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── PUT /api/cows/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCow_GenderAsString_UpdatesGender()
    {
        var cow = CreateCow(); // starts as Female
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var json = $$"""
            {
                "gender": "male",
                "birthDate": "2021-05-10",
                "bodyTemp": 39.0,
                "latitude": 51.5,
                "longitude": 0.1
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.UpdateCow(req, cow.CowId);
        Assert.IsType<OkObjectResult>(result);
        var updated = await _db.Cows.FindAsync(cow.CowId);
        Assert.Equal(Gender.Male, updated!.Gender);
    }

    [Fact]
    public async Task UpdateCow_InvalidGenderString_ReturnsBadRequest()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var json = """
            {
                "gender": "unknown",
                "birthDate": "2021-05-10",
                "bodyTemp": 38.5,
                "latitude": 51.5,
                "longitude": 0.1
            }
            """;

        var req = CreateHttpRequestWithBody(json);
        var result = await _sut.UpdateCow(req, cow.CowId);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateCow_NotFound_ReturnsNotFound()
    {
        var req = CreateHttpRequestWithBody("""
            {
                "gender": "female",
                "birthDate": "2021-05-10",
                "bodyTemp": 38.5,
                "latitude": 51.5,
                "longitude": 0.1
            }
            """);
        var result = await _sut.UpdateCow(req, Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    // ─── GET /api/cows/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCow_Exists_ReturnsOk()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.GetCow(CreateHttpRequest(), cow.CowId);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCow_NotFound_Returns404()
    {
        var result = await _sut.GetCow(CreateHttpRequest(), Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    // ─── DELETE /api/cows/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCow_Exists_Returns204()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteCow(CreateHttpRequest(), cow.CowId);
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await _db.Cows.CountAsync());
    }

    [Fact]
    public async Task DeleteCow_NotFound_Returns404()
    {
        var result = await _sut.DeleteCow(CreateHttpRequest(), Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    // ─── GET /api/cows/{cowId}/vaccinations ──────────────────────────────────

    [Fact]
    public async Task GetVaccinations_CowWithRecords_ReturnsList()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        _db.VaccinationRecords.Add(new VaccinationRecord
        {
            CowId = cow.CowId,
            VaccineName = "FMD",
            AdministeredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1))
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetVaccinations(CreateHttpRequest(), cow.CowId) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(result!.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetVaccinations_CowNotFound_Returns404()
    {
        var result = await _sut.GetVaccinations(CreateHttpRequest(), Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── POST /api/cows/{cowId}/vaccinations ─────────────────────────────────

    [Fact]
    public async Task AddVaccination_Valid_Returns201()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var json = """
            {
                "vaccineName": "FMD",
                "administeredDate": "2026-01-15"
            }
            """;
        var result = await _sut.AddVaccination(CreateHttpRequestWithBody(json), cow.CowId);
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        Assert.Equal(1, await _db.VaccinationRecords.CountAsync());
    }

    [Fact]
    public async Task AddVaccination_InvalidBody_Returns400()
    {
        var cow = CreateCow();
        _db.Cows.Add(cow);
        await _db.SaveChangesAsync();

        var result = await _sut.AddVaccination(CreateHttpRequestWithBody("null"), cow.CowId);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddVaccination_CowNotFound_Returns404()
    {
        var json = """
            {
                "vaccineName": "FMD",
                "administeredDate": "2026-01-15"
            }
            """;
        var result = await _sut.AddVaccination(CreateHttpRequestWithBody(json), Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── PUT /api/alerts/{id}/resolve ────────────────────────────────────────

    [Fact]
    public async Task ResolveAlert_Exists_ReturnsOk()
    {
        var alert = new Alert { CowId = Guid.NewGuid(), AlertType = AlertType.HighTemperature, Message = "Hot" };
        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync();

        var result = await _sut.ResolveAlert(CreateHttpRequest(), alert.AlertId) as OkObjectResult;
        Assert.NotNull(result);
        Assert.True(((Alert)result!.Value!).IsResolved);
    }

    [Fact]
    public async Task ResolveAlert_NotFound_Returns404()
    {
        var result = await _sut.ResolveAlert(CreateHttpRequest(), Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    // ─── GET /api/alerts ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAlerts_NoAlerts_ReturnsEmptyList()
    {
        var result = await _sut.GetAlerts(CreateHttpRequest()) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<Alert>>(result.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAlerts_OnlyReturnsUnresolved()
    {
        var cowId = Guid.NewGuid();
        _db.Alerts.AddRange(
            new Alert { CowId = cowId, AlertType = AlertType.HighTemperature, Message = "Hot", IsResolved = false },
            new Alert { CowId = cowId, AlertType = AlertType.HighTemperature, Message = "Hot resolved", IsResolved = true }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAlerts(CreateHttpRequest()) as OkObjectResult;
        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IEnumerable<Alert>>(result.Value).ToList();
        Assert.Single(list);
        Assert.False(list[0].IsResolved);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Cow CreateCow() => new()
    {
        Gender = Gender.Female,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2))
    };

    private static HttpRequest CreateHttpRequestWithBody(string json)
    {
        var ctx = new DefaultHttpContext();
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentType = "application/json";
        return ctx.Request;
    }
}
