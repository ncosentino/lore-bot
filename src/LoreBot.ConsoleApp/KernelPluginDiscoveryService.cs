using Microsoft.SemanticKernel;

namespace LoreBot.ConsoleApp;

internal sealed class KernelPluginDiscoveryService
{
    public List<PluginInfo> DiscoverPlugins(Kernel kernel)
    {
        var plugins = new List<PluginInfo>();
        
        foreach (var plugin in kernel.Plugins)
        {
            plugins.Add(new PluginInfo(plugin.Name, plugin.Description));
        }
        
        return plugins;
    }

    public List<FunctionInfo> DiscoverFunctions(Kernel kernel)
    {
        var functions = new List<FunctionInfo>();
        
        foreach (var plugin in kernel.Plugins)
        {
            foreach (var function in plugin)
            {
                var parameters = new List<ParameterInfo>();
                
                foreach (var param in function.Metadata.Parameters)
                {
                    parameters.Add(new ParameterInfo(
                        param.Name,
                        param.ParameterType ?? typeof(string),
                        param.Description,
                        param.IsRequired,
                        param.DefaultValue
                    ));
                }
                
                functions.Add(new FunctionInfo(
                    plugin.Name,
                    function.Name,
                    function.Description,
                    parameters
                ));
            }
        }
        
        return functions;
    }

    public KernelFunction? GetFunction(Kernel kernel, string pluginName, string functionName)
    {
        var plugin = kernel.Plugins.FirstOrDefault(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        if (plugin == null) return null;
        
        return plugin.FirstOrDefault(f => f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record PluginInfo(
    string Name, 
    string? Description);

public sealed record FunctionInfo(
    string PluginName, 
    string FunctionName, 
    string? Description, 
    List<ParameterInfo> Parameters);

public sealed record ParameterInfo(
    string Name, 
    Type Type, 
    string? Description, 
    bool IsRequired, 
    object? DefaultValue);