using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using MilkHisab.Api.Contracts;
using MilkHisab.Api.Data;
using Microsoft.AspNetCore.Mvc;
using MilkHisab.Api.Services;
using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql;

namespace MilkHisab.Api.Extensions;

/// <summary>
/// Hybrid DI: Controller â†’ IService â†’ Service(AppDbContext). No MediatR / repositories.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMilkHisabCore(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var messages = context.ModelState
                    .Where(kv => kv.Value?.Errors.Count > 0)
                    .SelectMany(kv => kv.Value!.Errors.Select(e =>
                        string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? $"{kv.Key} is invalid."
                            : e.ErrorMessage))
                    .Distinct()
                    .ToList();
                var message = messages.Count == 0 ? "Invalid request." : string.Join(" ", messages);
                return new BadRequestObjectResult(new ApiMessage(message));
            };
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Turso remote libSQL (not local SQLite file)
        var tursoCs = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(tursoCs))
        {
            var url = config["Turso:Url"];
            var token = config["Turso:AuthToken"];
            if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(token))
                tursoCs = $"Data Source={url};Auth Token={token}";
        }
        if (string.IsNullOrWhiteSpace(tursoCs))
            throw new InvalidOperationException(
                "Turso connection missing. Set ConnectionStrings:Default or Turso:Url + Turso:AuthToken in appsettings.Secrets.json");

        services.AddDbContext<AppDbContext>(options =>
            options.UseLibSql(tursoCs));

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IDailyEntryService, DailyEntryService>();
        services.AddScoped<IHisabService, HisabService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IPersonalService, PersonalService>();
        services.AddScoped<IAccountService, AccountService>();
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        var jwtOptions = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = JwtTokenService.CreateValidationParameters(jwtOptions);
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsync("""{"message":"Sign in required."}""");
                    }
                };
            });
        services.AddAuthorization();
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(config["DataProtection:KeyPath"] ?? Path.Combine(AppContext.BaseDirectory, "keys")));
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AngularDev", policy =>
                policy.WithOrigins(
                        "http://localhost:4200",
                        "https://hisab-daily.netlify.app",
                        "https://hisab-daily.netlify.app")
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .AllowAnyMethod());
        });

        return services;
    }
}