using NexusLabs.Needlr.AspNet;

using Serilog;

namespace LoreBot.WebApp;

internal sealed class SerilogPlugin : IWebApplicationPlugin
{
    public void Configure(WebApplicationPluginOptions options)
    {
        options.WebApplication.UseSerilogRequestLogging();
    }
}