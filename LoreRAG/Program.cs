using NexusLabs.Needlr.AspNet;
using NexusLabs.Needlr.Injection;

var webApp = new Syringe()
    .UsingAssemblyProvider(builder => builder
        .MatchingAssemblies(
            directory: AppDomain.CurrentDomain.BaseDirectory,
            fileFilter: file =>
                file.Contains("LoreRAG", StringComparison.OrdinalIgnoreCase) ||
                file.Contains("Needlr", StringComparison.OrdinalIgnoreCase))
        .Build())
    .BuildWebApplication();
webApp.Run();