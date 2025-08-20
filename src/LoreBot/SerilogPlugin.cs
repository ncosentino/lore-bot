using NexusLabs.Needlr;

using Serilog;
using Serilog.Events;

namespace LoreBot;

internal sealed class SerilogPlugin : IPostBuildServiceCollectionPlugin
{

    public void Configure(PostBuildServiceCollectionPluginOptions options)
    {
        var serilogSection = options.Config.GetSection("Serilog");

        var logLevel = serilogSection["MinimumLevel"] ?? "Information";
        var logPath = serilogSection["LogPath"] ?? "logs/app-.log";
        if (!Path.IsPathRooted(logPath))
        {
            logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath);
        }

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
    }
}