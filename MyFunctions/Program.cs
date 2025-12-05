// Program.cs (Function App - net8 isolated worker)
using Ashish_Backend_Folio.Data;
using Ashish_Backend_Folio.Data.Services.Implementation;
using Ashish_Backend_Folio.Data.Services.Interfaces;
using ashish_folio.data.Services.Implementation;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;
        var connString = config.GetConnectionString("DefaultConnection");


        // DbContext - uses connection string from local.settings.json or Azure App Settings
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString(connString)),
            ServiceLifetime.Scoped);

        services.AddScoped<IMessageProcessingTracker, EfMessageProcessingTracker>();
        services.AddScoped<IOrderService, OrderService>();

        // ServiceBus client (singleton) - prefer connection string in local dev; MI in production
        var sbNs = config["ServiceBusConnection"];
      
        services.AddSingleton(sp => new ServiceBusClient(sbNs, new DefaultAzureCredential()));
        

        // optional: if you want to reuse publisher code inside function app
        // services.AddScoped<IEventPublisher, ServiceBusRawPublisher>();
    })
    .Build();

host.Run();
