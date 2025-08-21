using Microsoft.SemanticKernel;

using Spectre.Console;

using System.Text.Json;

namespace LoreBot.ConsoleApp;

internal sealed class FunctionExecutor(
    KernelPluginDiscoveryService _discoveryService)
{
    public async Task<string?> ExecuteFunctionAsync(Kernel kernel, string pluginName, string functionName)
    {
        var function = _discoveryService.GetFunction(kernel, pluginName, functionName);
        if (function == null)
        {
            AnsiConsole.MarkupLine("[red]Function not found![/]");
            return null;
        }

        var functions = _discoveryService.DiscoverFunctions(kernel);
        var functionInfo = functions.FirstOrDefault(f => 
            f.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase) && 
            f.FunctionName.Equals(functionName, StringComparison.OrdinalIgnoreCase));
        
        if (functionInfo == null)
        {
            AnsiConsole.MarkupLine("[red]Function metadata not found![/]");
            return null;
        }

        var arguments = new KernelArguments();
        
        // Collect parameters from user
        if (functionInfo.Parameters.Any(p => p.Name != "kernel"))
        {
            AnsiConsole.MarkupLine($"\n[yellow]Enter parameters for {functionName}:[/]");
            
            foreach (var param in functionInfo.Parameters.Where(p => p.Name != "kernel"))
            {
                var value = CollectParameter(param);
                if (value != null)
                {
                    arguments[param.Name] = value;
                }
            }
        }

        // Execute the function
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"Executing {functionName}...", async ctx =>
            {
                await Task.Delay(100); // Small delay for visual effect
            });

        try
        {
            var result = await function.InvokeAsync(kernel, arguments);
            return result?.ToString();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error executing function: {ex.Message}[/]");
            return null;
        }
    }

    private object? CollectParameter(ParameterInfo param)
    {
        var prompt = $"[cyan]{param.Name}[/]";
        if (!string.IsNullOrEmpty(param.Description))
        {
            prompt += $" ([dim]{param.Description}[/])";
        }
        
        if (!param.IsRequired && param.DefaultValue != null)
        {
            prompt += $" [dim][[default: {param.DefaultValue}]][/]";
        }
        
        prompt += ": ";

        if (param.Type == typeof(bool))
        {
            return AnsiConsole.Confirm(prompt, param.DefaultValue as bool? ?? false);
        }
        else if (param.Type == typeof(int))
        {
            var input = AnsiConsole.Ask<string>(prompt);
            if (string.IsNullOrWhiteSpace(input) && param.DefaultValue != null)
            {
                return param.DefaultValue;
            }
            return int.TryParse(input, out var intValue) ? intValue : param.DefaultValue;
        }
        else if (param.Type == typeof(double))
        {
            var input = AnsiConsole.Ask<string>(prompt);
            if (string.IsNullOrWhiteSpace(input) && param.DefaultValue != null)
            {
                return param.DefaultValue;
            }
            return double.TryParse(input, out var doubleValue) ? doubleValue : param.DefaultValue;
        }
        else // Default to string
        {
            var input = AnsiConsole.Ask<string>(prompt);
            if (string.IsNullOrWhiteSpace(input) && param.DefaultValue != null)
            {
                return param.DefaultValue;
            }
            return string.IsNullOrWhiteSpace(input) ? null : input;
        }
    }

    public void DisplayResult(string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            AnsiConsole.MarkupLine("[yellow]No result returned[/]");
            return;
        }

        // Try to pretty print JSON
        try
        {
            var json = JsonDocument.Parse(result);
            var prettyJson = JsonSerializer.Serialize(json, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            // Use Text instead of Panel with string to avoid markup parsing
            var panel = new Panel(new Text(prettyJson))
            {
                Header = new PanelHeader("Result", Justify.Center),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1)
            };
            
            AnsiConsole.Write(panel);
        }
        catch
        {
            // If not JSON, display as plain text using Text to avoid markup parsing
            var panel = new Panel(new Text(result))
            {
                Header = new PanelHeader("Result", Justify.Center),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 1)
            };
            
            AnsiConsole.Write(panel);
        }
    }
}