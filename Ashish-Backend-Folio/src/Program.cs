using Ashish_Backend_Folio.Data;
using Ashish_Backend_Folio.Interfaces;
using Ashish_Backend_Folio.Messaging;
using Ashish_Backend_Folio.Messaging.Policies;
using Ashish_Backend_Folio.Middlewares;
using Ashish_Backend_Folio.Models;
using Ashish_Backend_Folio.Repositories.Implementation;
using Ashish_Backend_Folio.Repositories.Interface;
using Ashish_Backend_Folio.Services.Implementation;
using Ashish_Backend_Folio.Storage.Implementation;
using Ashish_Backend_Folio.Storage.Interface;
using Ashish_Backend_Folio.Storage.Models;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

//Cloud Services
var kvUrl = builder.Configuration["KeyVault:VaultUri"]; // e.g., https://myvault.vault.azure.net/
builder.Services.AddSingleton(new SecretClient(new Uri(kvUrl), new DefaultAzureCredential()));

builder.Services.AddSingleton(sp =>
    new BlobServiceClient(new Uri(builder.Configuration["Blob:ServiceUri"]), new DefaultAzureCredential()));
builder.Services.AddScoped<IBlobService,BlobService>();

builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

//messaging
var useServiceBus = builder.Configuration.GetValue<bool>("Messaging:UseServiceBus");

if (useServiceBus)
{
    // ServiceBusClient registration (singleton)
    var sbConn = builder.Configuration["ServiceBus:ConnectionString"];
    if (!string.IsNullOrEmpty(sbConn))
        builder.Services.AddSingleton(_ => new ServiceBusClient(sbConn));
    else
        builder.Services.AddSingleton(sp => new ServiceBusClient(builder.Configuration["ServiceBus:Namespace"], new DefaultAzureCredential()));

    // raw publisher
    builder.Services.AddSingleton<ServiceBusRawPublisher>();

    // core pipeline: raw -> idempotency -> retry -> fallback -> telemetry
    builder.Services.AddSingleton<IEventPublisher>(sp =>
    {
        IEventPublisher raw = sp.GetRequiredService<ServiceBusRawPublisher>();
        IEventPublisher idemp = new IdempotencyDecorator(raw);
        IEventPublisher retry = new RetryPublisherDecorator(idemp, sp.GetRequiredService<ILogger<RetryPublisherDecorator>>());
        IEventPublisher fallback = new FallbackDecorator(retry, sp.GetRequiredService<IFailedMessageStore>(), sp.GetRequiredService<ILogger<FallbackDecorator>>());
        // Wrap telemetry last or first depending on needs
        return fallback;
    });
}
else
{
    // Replace with Kafka/other provider registration
    //builder.Services.AddSingleton<IEventPublisher, KafkaPublisher>();
}




// Application services
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//service bus
builder.Services.AddSingleton<IEventPublisher, ServiceBusRawPublisher>();



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger / OpenAPI (Swashbuckle)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ashish Backend Folio API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token only. 'Bearer' prefix will be added automatically."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ======================================================
//  JWT Options (mapped to POCO)
// ======================================================
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// ======================================================
//  Secret Provider abstraction
// ======================================================
builder.Services.AddSingleton<ISecretProvider, KeyVaultSecretProvider>();

// ======================================================
//  JWT TokenValidationParameters configuration (lazy-loaded)
// ======================================================
builder.Services.AddSingleton<TokenValidationParameters>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
    var secretProvider = sp.GetRequiredService<ISecretProvider>();

    // fetch from Key Vault
    var signingKey = secretProvider
        .GetSecretAsync(opts.SigningKeySecretName)
        .GetAwaiter()
        .GetResult();

    return new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = opts.Issuer,
        ValidateAudience = true,
        ValidAudience = opts.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});



builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
     var sp = builder.Services.BuildServiceProvider(); // only acceptable at startup
     options.TokenValidationParameters = sp.GetRequiredService<TokenValidationParameters>();
 });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy =>
    policy.RequireRole("Admin"));

var app = builder.Build();

// Apply migrations & seed roles/user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    await SeedData.SeedRolesAndAdminAsync(roleManager, userManager);
}

if (app.Environment.IsDevelopment())
{
    // Expose the built-in OpenAPI document at /openapi/v1.json
    app.UseSwagger();

    // Use Swagger UI package to display it
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ashish Backend Folio API v1");
    });
}
// Source - https://stackoverflow.com/a
// Posted by Jeremy S., modified by community. See post 'Timeline' for change history
// Retrieved 2025-11-22, License - CC BY-SA 4.0

app.UseDeveloperExceptionPage();


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseJwtValidation();
app.UseAuthorization();

app.MapControllers();

app.Run();
