using InventoryAlert.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace InventoryAlert.Infrastructure.Utilities;

public static class LoggingConfiguration
{
    public const string ConsoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}";
    public const string FileOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

    // Standardized Log Message Templates
    public static class Templates
    {
        public const string ApiRequest = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F3}ms | User: {UserId} | Req: {@RequestBody} | Res: {@ResponseBody}";
        public const string ApiRequestMinimal = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F3}ms | User: {UserId}";
        public const string JobStarted = "handler.started: Job {JobName} execution started.";
        public const string JobCompleted = "handler.completed: Job {JobName} execution finished. Succeeded={Succeeded} | ElapsedMs={ElapsedMs:F3}";
        public const string UnhandledException = "An unhandled exception has occurred while executing the request.";
    }

    public static LoggerConfiguration ApplyBaseConfiguration(this LoggerConfiguration loggerConfiguration, AppSettings settings, string serviceName)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                  ?? "Production";

        return loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("Environment", env)
            .WriteTo.Console(outputTemplate: ConsoleOutputTemplate)
            .WriteTo.Seq(settings.Seq.ServerUrl)
            .WriteTo.File($"logs/{serviceName.ToLower()}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: FileOutputTemplate);
    }

    public static void AddCorrelationEnricher(this IServiceCollection services)
    {
        services.AddSingleton<CorrelationIdEnricher>();
    }
}
