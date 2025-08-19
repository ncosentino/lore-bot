namespace LoreRAG.Configuration;

public class EmbeddingConfiguration
{
    public const string SectionName = "Embedding";
    
    public string Provider { get; set; } = "azure-openai";
    public int Dimensions { get; set; } = 768;
    public int MaxTokensPerChunk { get; set; } = 2000;
    public int TargetTokensPerChunk { get; set; } = 400;
    public int OverlapTokens { get; set; } = 75;
    public AzureOpenAIEmbeddingConfig? AzureOpenAI { get; set; }
    public OpenAIEmbeddingConfig? OpenAI { get; set; }
    
    public void Validate()
    {
        if (Dimensions <= 0)
            throw new InvalidOperationException("Embedding Dimensions must be greater than 0");
        
        if (MaxTokensPerChunk <= 0)
            throw new InvalidOperationException("MaxTokensPerChunk must be greater than 0");
        
        if (TargetTokensPerChunk <= 0)
            throw new InvalidOperationException("TargetTokensPerChunk must be greater than 0");
        
        if (TargetTokensPerChunk > MaxTokensPerChunk)
            throw new InvalidOperationException("TargetTokensPerChunk cannot be greater than MaxTokensPerChunk");
        
        if (OverlapTokens < 0)
            throw new InvalidOperationException("OverlapTokens cannot be negative");
        
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
                throw new InvalidOperationException($"Invalid embedding provider: {Provider}. Supported: azure-openai, openai");
        }
    }
}

public class AzureOpenAIEmbeddingConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Deployment { get; set; } = string.Empty;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Azure OpenAI Embedding Endpoint is required");
        
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("Azure OpenAI Embedding Key is required");
        
        if (string.IsNullOrWhiteSpace(Deployment))
            throw new InvalidOperationException("Azure OpenAI Embedding Deployment is required");
    }
}

public class OpenAIEmbeddingConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-3-small";
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("OpenAI Embedding API Key is required");
        
        if (string.IsNullOrWhiteSpace(Model))
            throw new InvalidOperationException("OpenAI Embedding Model is required");
    }
}