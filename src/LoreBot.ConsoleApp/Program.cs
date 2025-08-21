using LoreBot;
using LoreBot.Bootstrap;
using LoreBot.ConsoleApp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Spectre.Console;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();
    var serviceProvider = new LoreBotSyringeBuilder()
        .Build()
        .BuildServiceProvider(configuration);

    var kernelFactory = serviceProvider.GetRequiredService<SemanticKernelFactory>();
    var kernel = kernelFactory.Build();

    var consoleUI = serviceProvider.GetRequiredService<ConsoleUI>();
    await consoleUI.RunAsync(kernel);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    Environment.Exit(1);
}