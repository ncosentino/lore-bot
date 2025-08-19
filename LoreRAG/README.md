# LoreRAG - Retrieval-Augmented Generation System

A high-performance RAG system for game lore knowledge bases, built with PostgreSQL/pgvector, Dapper, Semantic Kernel, and Serilog.

## Features

- **Hybrid Search**: Combines vector similarity search with PostgreSQL full-text search for optimal retrieval
- **Scalable Storage**: PostgreSQL with pgvector extension using HNSW indexing
- **Plugin Architecture**: Modular design with single-responsibility plugins using Needlr pattern
- **Azure OpenAI Integration**: Semantic Kernel integration for embeddings and chat completions
- **Markdown Ingestion**: Smart chunking with heading preservation and overlap
- **Health Monitoring**: Built-in health checks for database and embedding services
- **Structured Logging**: Serilog with console and rolling file outputs

## Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose
- Azure OpenAI account (or compatible provider)
- PostgreSQL 16 with pgvector extension (provided via Docker)

## Quick Start

### 1. Clone and Setup

```bash
git clone <repository>
cd LoreRAG
```

### 2. Configure Environment

Copy the example environment file and configure your settings:

```bash
cp .env.example .env
```

Edit `.env` with your Azure OpenAI credentials:
- `AZURE_OPENAI_ENDPOINT`: Your Azure OpenAI endpoint
- `AZURE_OPENAI_KEY`: Your API key
- `AZURE_OPENAI_CHAT_DEPLOYMENT`: Chat model deployment name
- `AZURE_OPENAI_EMBED_DEPLOYMENT`: Embedding model deployment name

### 3. Start PostgreSQL

```bash
docker-compose up -d
```

This will:
- Start PostgreSQL 16 with pgvector extension
- Create the `lore` database
- Initialize the schema with tables and indexes

### 4. Run the Application

```bash
dotnet run
```

The API will be available at:
- Swagger UI: http://localhost:5000
- Health Check: http://localhost:5000/health
- API Endpoint: http://localhost:5000/api/lore/ask

## API Endpoints

### Search Lore Knowledge Base

```http
GET /api/lore/ask?q=<query>&k=<num_results>
```

Parameters:
- `q` (required): Search query
- `k` (optional): Number of results (default: 6, max: 20)

Response:
```json
{
  "question": "worldstone origin",
  "hits": [
    {
      "id": 123,
      "sourcePath": "lore/worldstone.md",
      "anchorId": "creation",
      "title": "The Creation of the Worldstone",
      "headings": ["History", "Creation"],
      "excerpt": "The Worldstone was created by...",
      "denseScore": 0.85,
      "sparseScore": 0.72,
      "score": 0.80
    }
  ],
  "generatedAtUtc": "2024-01-19T10:00:00Z"
}
```

### Health Checks

- `/health` - Overall system health
- `/health/ready` - Readiness probe (DB + AI services)
- `/health/live` - Liveness probe

## Ingesting Content

Place your Markdown files in a directory and use the ingestion service:

```bash
# Via API (requires authentication in production)
curl -X POST http://localhost:5000/api/ingest?path=/path/to/markdown/files
```

The ingester will:
- Scan for `.md` files recursively
- Strip front matter
- Chunk by headings with overlap
- Generate embeddings via Azure OpenAI
- Store in PostgreSQL with deduplication

## Architecture

### Components

1. **Plugins** (Single Responsibility)
   - `PostgresConnectionPlugin`: Database connections with pgvector
   - `DapperVectorTypeHandlerPlugin`: Vector type mapping for Dapper
   - `SerilogPlugin`: Structured logging configuration
   - `SemanticKernelAzureOpenAIPlugin`: Azure OpenAI integration
   - `SemanticKernelProviderSelectorPlugin`: Provider abstraction

2. **Data Access**
   - `ILoreRepository`: Dapper-based data operations
   - Hybrid search SQL combining vector and FTS

3. **Services**
   - `IEmbeddingService`: Text embedding generation
   - `ILoreRetriever`: Query orchestration
   - `IngestionService`: Markdown processing pipeline
   - `MarkdownChunker`: Smart text chunking

4. **API**
   - RESTful endpoints with DTOs
   - Semantic Kernel functions for tool use
   - OpenAPI/Swagger documentation

### Database Schema

```sql
lore_chunks (
  id           bigserial PRIMARY KEY,
  source_path  text NOT NULL,
  anchor_id    text,
  title        text,
  headings     text[],
  content      text NOT NULL,
  tokens       int,
  word_count   int,
  links_to     text[],
  updated_at   timestamptz NOT NULL DEFAULT NOW(),
  embedding    vector(768) NOT NULL,
  tsv          tsvector
)
```

Indexes:
- HNSW on `embedding` for vector search
- GIN on `tsv` for full-text search
- BTREE on `source_path` for filtering

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DB_CONN` | PostgreSQL connection string | localhost connection |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint | Required |
| `AZURE_OPENAI_KEY` | API key | Required |
| `AZURE_OPENAI_CHAT_DEPLOYMENT` | Chat model deployment | gpt-4o |
| `AZURE_OPENAI_EMBED_DEPLOYMENT` | Embedding model deployment | text-embedding-3-large |
| `AI_PROVIDER` | AI provider selection | azure-openai |
| `LOG_LEVEL` | Serilog minimum level | Information |
| `LOG_PATH` | Log file path pattern | logs/app-.log |

### Performance Tuning

- **Vector Dimension**: 768 (configurable in schema)
- **Chunk Size**: 300-600 tokens with 50-100 token overlap
- **Hybrid Weights**: 65% dense, 35% sparse (configurable)
- **Connection Pooling**: Enabled by default

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Docker Operations

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f postgres

# Stop services
docker-compose down

# Clean volumes
docker-compose down -v
```

## Production Considerations

1. **Security**
   - Add authentication/authorization
   - Use secrets management for API keys
   - Enable HTTPS only
   - Restrict CORS policies

2. **Scaling**
   - Use connection pooling
   - Implement caching layer
   - Consider read replicas for queries
   - Use background jobs for ingestion

3. **Monitoring**
   - Export metrics to Prometheus
   - Use distributed tracing
   - Set up alerting
   - Monitor embedding costs

## Troubleshooting

### Database Connection Issues
- Verify PostgreSQL is running: `docker-compose ps`
- Check connection string in environment
- Ensure pgvector extension is installed

### Embedding Failures
- Verify Azure OpenAI credentials
- Check deployment names match
- Monitor rate limits and quotas

### Search Performance
- Tune HNSW parameters
- Adjust chunk sizes
- Monitor index statistics
- Consider parallel processing

## License

[Your License Here]

## Contributing

[Your Contributing Guidelines Here]