using NexusLabs.Needlr.AspNet;

using Serilog;
using Serilog.Events;

namespace LoreRAG;

internal sealed class SerilogPlugin :
    IWebApplicationBuilderPlugin,
    IWebApplicationPlugin
{
    public void Configure(WebApplicationBuilderPluginOptions options)
    {
        // Read from the Serilog section in configuration
        var serilogSection = options.Builder.Configuration.GetSection("Serilog");

        var logLevel = serilogSection["MinimumLevel"] ?? "Information";
        var logPath = serilogSection["LogPath"] ?? "logs/app-.log";

        var minimumLevel = Enum.TryParse<LogEventLevel>(logLevel, out var level)
            ? level
            : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Serilog initialized with level {LogLevel} and path {LogPath}", logLevel, logPath);

        options.Builder.Host.UseSerilog();
    }

    public void Configure(WebApplicationPluginOptions options)
    {
        options.WebApplication.UseSerilogRequestLogging();
    }
}