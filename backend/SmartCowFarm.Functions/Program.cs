using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Services;
using System.Text.Json.Serialization;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<CowFarmDbContext>(options =>
            options.UseSqlServer(
                context.Configuration["SqlConnectionString"],
                sql => sql.EnableRetryOnFailure()));

        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
        {
            o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        });

        services.AddScoped<ICowService, CowService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<NotificationService>();
    })
    .Build();

if (host.Services.GetRequiredService<IConfiguration>().GetValue<bool>("AutoCreateDatabase"))
{
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CowFarmDbContext>();
    db.Database.EnsureCreated();
}

host.Run();
