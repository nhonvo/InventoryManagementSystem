using Hangfire;
using Hangfire.PostgreSql;
using InventoryAlert.Api.Configuration;
using InventoryAlert.Api.Extensions;
using InventoryAlert.Api.Middleware;
using InventoryAlert.Api.ServiceExtensions;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Scalar.AspNetCore;
using Serilog;

// ─── Early Configuration Binding for Bootstrap ───────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.Get<ApiSettings>()
    ?? throw new InvalidOperationException("AppSettings configuration is missing.");

// ─── Serilog bootstrap ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ApplyBaseConfiguration(settings, "InventoryAlert.Api")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ApplyBaseConfiguration(settings, "InventoryAlert.Api")
            .ReadFrom.Services(services)
            .Enrich.With(services.GetRequiredService<CorrelationIdEnricher>());
    });

    // ─── DI Registrations ─────────────────────────────────────────────────────
    builder.Services.AddSingleton(settings);
    builder.Services.AddSingleton<AppSettings>(settings);
    builder.Services.AddCorrelationEnricher();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddTransient<GlobalExceptionMiddleware>();
    builder.Services.AddTransient<ApiLoggingMiddleware>();
    builder.Services.AddTransient<CorrelationIdMiddleware>();

    // ─── Security / Auth / CORS ───────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy => policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());

        options.AddPolicy("AllowAll", policy => policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var jwtKey = settings.Jwt.Key;
    if (string.IsNullOrEmpty(jwtKey))
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Docker"))
        {
            jwtKey = "InventoryAlert_Temporary_Default_Key_For_Dev_Only_1234567890";
            Log.Warning("Using temporary default JWT key. NOT FOR PRODUCTION.");
        }
        else
        {
            throw new InvalidOperationException("Jwt:Key is required in configuration.");
        }
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments(InventoryAlert.Domain.Interfaces.SignalRConstants.NotificationHubRoute))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddSignalR()
        .AddStackExchangeRedis(settings.Redis.ConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("InventoryAlert_SignalR");
        });

    builder.Services.AddHttpClient();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerOpenAPI(settings);
    builder.Services.SetupMvc();
    builder.Services.AddCompressionCustom();
    builder.Services.SetupHealthCheck(settings);
    builder.Services.AddResponseCaching();
    builder.Services.AddWebApiInfrastructure(settings);

    builder.Services.AddHangfire(config =>
        config.UsePostgreSqlStorage(opts =>
                opts.UseNpgsqlConnection(settings.Database.DefaultConnection)));

    var app = builder.Build();

    // ─── Auto-migrate & Seed Database ─────────────────────────────────────────
    var retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2),
            (exception, timeSpan, retryCount, context) =>
            {
                app.Logger.LogWarning("Database migration attempt {RetryCount} failed: {Message}", retryCount, exception.Message);
            });

    await retryPolicy.ExecuteAsync(async () =>
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        if (Environment.GetEnvironmentVariable("SKIP_SEEDING") != "true")
        {
            await InventoryAlert.Infrastructure.Persistence.Postgres.DatabaseSeeder.SeedAsync(dbContext, app.Logger);
        }
    });

    // ─── Pipeline ─────────────────────────────────────────────────────────────
    app.UseCors("AllowAll");
    app.UseCors();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseResponseCompression();
    app.UseStaticFiles();

    app.UseRouting();
    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<ApiLoggingMiddleware>();

    app.UseResponseCaching();

    // Enable Swagger UI and Scalar API reference across all environments (including Production demo)
    app.UseSwaggerWithUI();

    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new InventoryAlert.Api.Filters.DevDashboardAuthorizationFilter() }
    });

    // Proxy /aws and /aws/* requests to internal Moto server on port 5000 (allows dynamodb-admin or AWS CLI remote management)
    app.UseWhen(context => context.Request.Path.StartsWithSegments("/aws"), awsApp =>
    {
        awsApp.Run(async context =>
        {
            var clientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = clientFactory.CreateClient();

            var path = context.Request.Path.Value ?? "/aws";
            var relativePath = path.Length >= 4 ? path[4..] : "";
            if (relativePath.StartsWith('/')) relativePath = relativePath[1..];

            var targetUri = new Uri($"http://127.0.0.1:5000/{relativePath}{context.Request.QueryString}");

            var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);
            foreach (var header in context.Request.Headers)
            {
                if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            if (context.Request.ContentLength > 0 || !string.IsNullOrEmpty(context.Request.ContentType))
            {
                requestMessage.Content = new StreamContent(context.Request.Body);
                if (!string.IsNullOrEmpty(context.Request.ContentType) && System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var contentType))
                {
                    requestMessage.Content.Headers.ContentType = contentType;
                }
            }

            using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            context.Response.StatusCode = (int)responseMessage.StatusCode;
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            await responseMessage.Content.CopyToAsync(context.Response.Body);
        });
    });

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("InventoryAlert API Reference")
            .WithTheme(ScalarTheme.Mars)
            .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.ConfigureHealthCheck();
    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
    app.MapControllers();
    app.MapHub<InventoryAlert.Infrastructure.Hubs.NotificationHub>(InventoryAlert.Domain.Interfaces.SignalRConstants.NotificationHubRoute);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

namespace InventoryAlert.Api
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class Program { }
}
