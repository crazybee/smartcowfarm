using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartCowFarm.Functions.Data;
using SmartCowFarm.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<CowFarmDbContext>(options =>
            options.UseSqlServer(
                context.Configuration["SqlConnectionString"],
                sql => sql.EnableRetryOnFailure()));

        services.AddScoped<NotificationService>();
    })
    .Build();

host.Run();
