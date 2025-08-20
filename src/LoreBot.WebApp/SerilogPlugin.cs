using NexusLabs.Needlr.AspNet;

using Serilog;

namespace LoreBot.WebApp;

internal sealed class SerilogPlugin :
    IWebApplicationBuilderPlugin,
    IWebApplicationPlugin
{
    public void Configure(WebApplicationBuilderPluginOptions options)
    {
        options.Builder.Host.UseSerilog();
    }

    public void Configure(WebApplicationPluginOptions options)
    {
        options.WebApplication.UseSerilogRequestLogging();
    }
}