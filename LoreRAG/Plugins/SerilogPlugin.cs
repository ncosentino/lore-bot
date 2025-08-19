using Serilog;
using Serilog.Events;

namespace LoreRAG.Plugins;

public class SerilogPlugin
{
    public static void ConfigureSerilog(IConfiguration configuration)
    {
        // Read from the Serilog section in configuration
        var serilogSection = configuration.GetSection("Serilog");
        
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
    }
}

public static class SerilogPluginExtensions
{
    public static IHostBuilder AddSerilogPlugin(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog();
    }

    public static WebApplicationBuilder AddSerilogPlugin(this WebApplicationBuilder builder)
    {
        SerilogPlugin.ConfigureSerilog(builder.Configuration);
        builder.Host.UseSerilog();
        return builder;
    }
}