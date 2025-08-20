using NexusLabs.Needlr.Injection;

namespace LoreBot.Bootstrap;

public sealed class LoreBotSyringeBuilder
{
    public Syringe Build()
    {
        var syringe = new Syringe()
            .UsingAssemblyProvider(builder => builder
                .MatchingAssemblies(
                    directory: AppDomain.CurrentDomain.BaseDirectory,
                    fileFilter: file =>
                        file.Contains("LoreBot", StringComparison.OrdinalIgnoreCase) ||
                        file.Contains("Needlr", StringComparison.OrdinalIgnoreCase))
                .Build());
        return syringe;
    }
}
