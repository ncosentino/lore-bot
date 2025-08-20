using LoreBot.Bootstrap;

using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();
var serviceProvider = new LoreBotSyringeBuilder().Build().BuildServiceProvider(configuration);

Console.WriteLine("// FIXME: implement the console app with Spectre Console!");