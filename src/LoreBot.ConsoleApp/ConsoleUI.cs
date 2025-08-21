using Microsoft.SemanticKernel;

using Spectre.Console;

namespace LoreBot.ConsoleApp;

internal sealed class ConsoleUI(
    KernelPluginDiscoveryService _discoveryService, 
    FunctionExecutor _functionExecutor)
{
    public async Task RunAsync(Kernel kernel)
    {
        DisplayWelcome();
        
        while (true)
        {
            var action = ShowMainMenu();
            
            switch (action)
            {
                case "Execute Function":
                    await ExecuteFunctionMenuAsync(kernel);
                    break;
                case "List All Functions":
                    DisplayAllFunctions(kernel);
                    break;
                case "List All Plugins":
                    DisplayAllPlugins(kernel);
                    break;
                case "Exit":
                    DisplayGoodbye();
                    return;
            }
            
            AnsiConsole.WriteLine();
            if (!AnsiConsole.Confirm("Continue?", true))
            {
                DisplayGoodbye();
                break;
            }
        }
    }

    private void DisplayWelcome()
    {
        AnsiConsole.Clear();
        
        var title = new FigletText("LoreBot")
            .Centered()
            .Color(Color.Cyan1);
        
        AnsiConsole.Write(title);
        
        var rule = new Rule("[cyan]Semantic Kernel Console Interface[/]")
        {
            Style = Style.Parse("cyan dim")
        };
        AnsiConsole.Write(rule);
        
        AnsiConsole.WriteLine();
    }

    private void DisplayGoodbye()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]Thank you for using LoreBot![/]");
        AnsiConsole.MarkupLine("[dim]Goodbye![/]");
    }

    private string ShowMainMenu()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]What would you like to do?[/]")
                .PageSize(10)
                .AddChoices(new[]
                {
                    "Execute Function",
                    "List All Functions",
                    "List All Plugins",
                    "Exit"
                }));
    }

    private async Task ExecuteFunctionMenuAsync(Kernel kernel)
    {
        var functions = _discoveryService.DiscoverFunctions(kernel);
        
        if (!functions.Any())
        {
            AnsiConsole.MarkupLine("[red]No functions found![/]");
            return;
        }

        // Group functions by plugin
        var groupedFunctions = functions
            .GroupBy(f => f.PluginName)
            .OrderBy(g => g.Key)
            .ToList();

        // Create a hierarchical menu
        var choices = new List<string>();
        var functionMap = new Dictionary<string, (string plugin, string function)>();
        
        foreach (var group in groupedFunctions)
        {
            foreach (var function in group.OrderBy(f => f.FunctionName))
            {
                var displayName = $"[cyan]{group.Key}[/] :: [yellow]{function.FunctionName}[/]";
                if (!string.IsNullOrEmpty(function.Description))
                {
                    displayName += $" [dim]- {function.Description}[/]";
                }
                
                choices.Add(displayName);
                functionMap[displayName] = (group.Key, function.FunctionName);
            }
        }

        choices.Add("[red]Cancel[/]");
        
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Select a function to execute:[/]")
                .PageSize(15)
                .AddChoices(choices));

        if (selected == "[red]Cancel[/]")
        {
            return;
        }

        if (functionMap.TryGetValue(selected, out var functionInfo))
        {
            var result = await _functionExecutor.ExecuteFunctionAsync(kernel, functionInfo.plugin, functionInfo.function);
            _functionExecutor.DisplayResult(result);
        }
    }

    private void DisplayAllFunctions(Kernel kernel)
    {
        var functions = _discoveryService.DiscoverFunctions(kernel);
        
        if (!functions.Any())
        {
            AnsiConsole.MarkupLine("[red]No functions found![/]");
            return;
        }

        var table = new Table();
        table.AddColumn(new TableColumn("Plugin").Centered());
        table.AddColumn(new TableColumn("Function").Centered());
        table.AddColumn("Description");
        table.AddColumn("Parameters");
        
        table.Border(TableBorder.Rounded);
        
        foreach (var function in functions.OrderBy(f => f.PluginName).ThenBy(f => f.FunctionName))
        {
            var parameters = string.Join(", ", 
                function.Parameters
                    .Where(p => p.Name != "kernel")
                    .Select(p => $"{p.Name}{(p.IsRequired ? "*" : "")}"));
            
            table.AddRow(
                $"[cyan]{function.PluginName}[/]",
                $"[yellow]{function.FunctionName}[/]",
                function.Description ?? "[dim]No description[/]",
                string.IsNullOrEmpty(parameters) ? "[dim]None[/]" : parameters
            );
        }
        
        AnsiConsole.Write(table);
    }

    private void DisplayAllPlugins(Kernel kernel)
    {
        var plugins = _discoveryService.DiscoverPlugins(kernel);
        
        if (!plugins.Any())
        {
            AnsiConsole.MarkupLine("[red]No plugins found![/]");
            return;
        }

        var table = new Table();
        table.AddColumn(new TableColumn("Plugin Name").Centered());
        table.AddColumn("Description");
        table.AddColumn("Function Count");
        
        table.Border(TableBorder.Rounded);
        
        foreach (var plugin in plugins.OrderBy(p => p.Name))
        {
            var functionCount = kernel.Plugins
                .FirstOrDefault(p => p.Name == plugin.Name)?
                .Count() ?? 0;
            
            table.AddRow(
                $"[cyan]{plugin.Name}[/]",
                plugin.Description ?? "[dim]No description[/]",
                functionCount.ToString()
            );
        }
        
        AnsiConsole.Write(table);
    }
}