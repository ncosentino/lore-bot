namespace LoreRAG.Configuration;

public class ChatConfiguration
{
    public const string SectionName = "Chat";
    
    public string Provider { get; set; } = "azure-openai";
    public AzureOpenAIChatConfig? AzureOpenAI { get; set; }
    public OpenAIChatConfig? OpenAI { get; set; }
    
    public void Validate()
    {
        switch (Provider?.ToLower())
        {
            case "azure-openai":
                if (AzureOpenAI == null)
                    throw new InvalidOperationException("AzureOpenAI configuration is required when Provider is 'azure-openai'");
                AzureOpenAI.Validate();
                break;
            case "openai":
                if (OpenAI == null)
                    throw new InvalidOperationException("OpenAI configuration is required when Provider is 'openai'");
                OpenAI.Validate();
                break;
            default:
                throw new InvalidOperationException($"Invalid chat provider: {Provider}. Supported: azure-openai, openai");
        }
    }
}

public class AzureOpenAIChatConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Deployment { get; set; } = string.Empty;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Azure OpenAI Endpoint is required");
        
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("Azure OpenAI Key is required");
        
        if (string.IsNullOrWhiteSpace(Deployment))
            throw new InvalidOperationException("Azure OpenAI Deployment is required");
    }
}

public class OpenAIChatConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o";
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("OpenAI API Key is required");
        
        if (string.IsNullOrWhiteSpace(Model))
            throw new InvalidOperationException("OpenAI Model is required");
    }
}