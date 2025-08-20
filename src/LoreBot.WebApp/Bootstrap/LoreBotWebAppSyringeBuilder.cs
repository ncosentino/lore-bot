using LoreBot.Bootstrap;

using NexusLabs.Needlr.AspNet;

namespace LoreBot.WebApp.Bootstrap;

public sealed class LoreBotWebAppSyringeBuilder
{
    public WebApplicationSyringe Build()
    {
        var syringe = new LoreBotSyringeBuilder()
            .Build()
            .ForWebApplication();
        return syringe;
    }
}
