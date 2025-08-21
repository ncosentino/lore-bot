namespace LoreBot.Configuration;

public class ChatConfiguration
{
    public const string SectionName = "Chat";
    
    public string Provider { get; set; } = "azure-openai";
    public AzureOpenAIChatConfig? AzureOpenAI { get; set; }
    public OpenAIChatConfig? OpenAI { get; set; }
    public OllamaChatConfig? Ollama { get; set; }
    
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
            case "ollama":
                if (Ollama == null)
                    throw new InvalidOperationException("Ollama configuration is required when Provider is 'ollama'");
                Ollama.Validate();
                break;
            default:
                throw new InvalidOperationException($"Invalid chat provider: {Provider}. Supported: azure-openai, openai, ollama");
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

public class OllamaChatConfig
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Ollama Endpoint is required");
        
        if (string.IsNullOrWhiteSpace(Model))
            throw new InvalidOperationException("Ollama Model is required");
        
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            throw new InvalidOperationException($"Ollama Endpoint '{Endpoint}' is not a valid URI");
    }
}