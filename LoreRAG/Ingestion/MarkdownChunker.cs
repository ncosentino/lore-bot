using LoreRAG.Configuration;

using Microsoft.Extensions.Options;

using System.Text;
using System.Text.RegularExpressions;

namespace LoreRAG.Ingestion;

public class MarkdownChunker
{
    private readonly int _targetTokens;
    private readonly int _overlapTokens;
    private readonly int _maxTokens;
    private readonly ILogger<MarkdownChunker> _logger;
    
    private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline);
    private static readonly Regex FrontMatterRegex = new(@"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
    private static readonly Regex CodeBlockRegex = new(@"```[\s\S]*?```", RegexOptions.Multiline);

    public MarkdownChunker(IOptions<EmbeddingConfiguration> options, ILogger<MarkdownChunker> logger)
    {
        var config = options.Value;
        _logger = logger;
        _targetTokens = config.TargetTokensPerChunk;
        _overlapTokens = config.OverlapTokens;
        _maxTokens = config.MaxTokensPerChunk;
        
        _logger.LogInformation("MarkdownChunker initialized with target: {Target}, max: {Max}, overlap: {Overlap} tokens",
            _targetTokens, _maxTokens, _overlapTokens);
    }

    public List<LoreChunk> ChunkMarkdownFile(string filePath, string content)
    {
        var chunks = new List<LoreChunk>();
        
        // Strip front matter
        content = StripFrontMatter(content);
        
        // Parse headings and structure
        var sections = ParseSections(content);
        
        // Create chunks from sections
        foreach (var section in sections)
        {
            var sectionChunks = CreateChunksFromSection(filePath, section);
            chunks.AddRange(sectionChunks);
        }
        
        _logger.LogInformation("Created {Count} chunks from {FilePath}", chunks.Count, filePath);
        return chunks;
    }

    private string StripFrontMatter(string content)
    {
        return FrontMatterRegex.Replace(content, "");
    }

    private List<MarkdownSection> ParseSections(string content)
    {
        var sections = new List<MarkdownSection>();
        var matches = HeadingRegex.Matches(content);
        
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var level = match.Groups[1].Value.Length;
            var title = match.Groups[2].Value.Trim();
            var startIndex = match.Index + match.Length;
            var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : content.Length;
            var sectionContent = content.Substring(startIndex, endIndex - startIndex).Trim();
            
            sections.Add(new MarkdownSection
            {
                Level = level,
                Title = title,
                Content = sectionContent,
                AnchorId = GenerateAnchorId(title)
            });
        }
        
        // If no headings, treat entire content as one section
        if (sections.Count == 0 && !string.IsNullOrWhiteSpace(content))
        {
            sections.Add(new MarkdownSection
            {
                Level = 0,
                Title = null,
                Content = content,
                AnchorId = null
            });
        }
        
        return sections;
    }

    private List<LoreChunk> CreateChunksFromSection(string filePath, MarkdownSection section)
    {
        var chunks = new List<LoreChunk>();
        var content = section.Content;
        
        // Estimate tokens (rough approximation: 1 token ≈ 4 characters)
        var estimatedTokens = content.Length / 4;
        
        if (estimatedTokens <= _targetTokens)
        {
            // Section fits in one chunk
            chunks.Add(CreateChunk(filePath, section, content));
        }
        else
        {
            // Split section into multiple chunks
            var paragraphs = SplitIntoParagraphs(content);
            var currentChunkContent = new StringBuilder();
            var currentTokens = 0;
            
            foreach (var paragraph in paragraphs)
            {
                var paragraphTokens = paragraph.Length / 4;
                
                // If a single paragraph exceeds max tokens, split it further
                if (paragraphTokens > _maxTokens)
                {
                    // Flush current chunk if not empty
                    if (currentChunkContent.Length > 0)
                    {
                        chunks.Add(CreateChunk(filePath, section, currentChunkContent.ToString().Trim()));
                        currentChunkContent.Clear();
                        currentTokens = 0;
                    }
                    
                    // Split large paragraph by sentences or at max token boundaries
                    var splitParagraphs = SplitLargeParagraph(paragraph, _targetTokens);
                    foreach (var splitPara in splitParagraphs)
                    {
                        chunks.Add(CreateChunk(filePath, section, splitPara.Trim()));
                    }
                    continue;
                }
                
                // Check if adding this paragraph would exceed target (or max) tokens
                if ((currentTokens + paragraphTokens > _targetTokens && currentChunkContent.Length > 0) ||
                    (currentTokens + paragraphTokens > _maxTokens))
                {
                    // Create chunk and start new one with overlap
                    chunks.Add(CreateChunk(filePath, section, currentChunkContent.ToString().Trim()));
                    
                    // Start new chunk with overlap from previous
                    currentChunkContent.Clear();
                    currentTokens = 0;
                    
                    // Add overlap from previous chunk if applicable
                    if (_overlapTokens > 0 && chunks.Count > 0)
                    {
                        var overlapText = GetOverlapText(chunks.Last().Content, _overlapTokens);
                        currentChunkContent.AppendLine(overlapText);
                        currentTokens = overlapText.Length / 4;
                    }
                }
                
                currentChunkContent.AppendLine(paragraph);
                currentTokens += paragraphTokens;
            }
            
            // Add remaining content
            if (currentChunkContent.Length > 0)
            {
                chunks.Add(CreateChunk(filePath, section, currentChunkContent.ToString().Trim()));
            }
        }
        
        return chunks;
    }

    private List<string> SplitIntoParagraphs(string content)
    {
        // Preserve code blocks as single units
        var codeBlocks = new Dictionary<string, string>();
        var codeBlockIndex = 0;
        
        content = CodeBlockRegex.Replace(content, match =>
        {
            var placeholder = $"__CODE_BLOCK_{codeBlockIndex}__";
            codeBlocks[placeholder] = match.Value;
            codeBlockIndex++;
            return placeholder;
        });
        
        // Split by double newlines
        var paragraphs = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
        
        // Restore code blocks
        for (int i = 0; i < paragraphs.Count; i++)
        {
            foreach (var kvp in codeBlocks)
            {
                paragraphs[i] = paragraphs[i].Replace(kvp.Key, kvp.Value);
            }
        }
        
        return paragraphs;
    }

    private string GetOverlapText(string content, int overlapTokens)
    {
        var overlapChars = overlapTokens * 4;
        if (content.Length <= overlapChars)
            return content;
        
        return content.Substring(content.Length - overlapChars);
    }

    private LoreChunk CreateChunk(string filePath, MarkdownSection section, string content)
    {
        var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var tokens = content.Length / 4; // Rough estimation
        
        return new LoreChunk
        {
            SourcePath = filePath,
            AnchorId = section.AnchorId,
            Title = section.Title,
            Headings = section.Title != null ? new[] { section.Title } : null,
            Content = content,
            Tokens = tokens,
            WordCount = wordCount,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private List<string> SplitLargeParagraph(string paragraph, int targetTokens)
    {
        var chunks = new List<string>();
        var targetChars = targetTokens * 4;
        
        // Try to split by sentences first
        var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+");
        var currentChunk = new StringBuilder();
        var currentChars = 0;
        
        foreach (var sentence in sentences)
        {
            var sentenceChars = sentence.Length;
            
            // If even a single sentence is too large, split it at word boundaries
            if (sentenceChars > targetChars)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                    currentChars = 0;
                }
                
                // Split sentence by words
                var words = sentence.Split(' ');
                foreach (var word in words)
                {
                    if (currentChars + word.Length + 1 > targetChars && currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.ToString().Trim());
                        currentChunk.Clear();
                        currentChars = 0;
                    }
                    
                    if (currentChunk.Length > 0) currentChunk.Append(' ');
                    currentChunk.Append(word);
                    currentChars += word.Length + 1;
                }
            }
            else if (currentChars + sentenceChars > targetChars && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
                currentChars = 0;
                currentChunk.Append(sentence);
                currentChars = sentenceChars;
            }
            else
            {
                if (currentChunk.Length > 0) currentChunk.Append(' ');
                currentChunk.Append(sentence);
                currentChars += sentenceChars + 1;
            }
        }
        
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }
        
        return chunks;
    }

    private string GenerateAnchorId(string title)
    {
        // Generate GitHub-style anchor IDs
        return title.ToLower()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "");
    }

    private class MarkdownSection
    {
        public int Level { get; set; }
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? AnchorId { get; set; }
    }
}