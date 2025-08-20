## PRD: Lore RAG System with Postgres/pgvector, Dapper, Needlr Plugins, Semantic Kernel, and Serilog

### Audience

This document is written for autonomous/assisted LLM agents and
developers. It specifies contracts, tasks, and exit criteria so agents
can build the system safely and in parallel.

------------------------------------------------------------------------

## 1) Overview

We will build a retrieval-augmented generation (RAG) system over a
Markdown lore wiki for a game world.\
Key stack decisions:

-   Storage: PostgreSQL with pgvector (HNSW) in Docker
-   Data access: Npgsql + Dapper (no EF Core)
-   Dependency injection: **Needlr** with **single-responsibility
    plugins** only
-   LLM integration: Semantic Kernel (SK), provider-pluggable starting
    with Azure OpenAI
-   Logging: Serilog
-   API returns **immutable sealed positional records** (DTOs)
-   Hybrid retrieval: vector similarity + Postgres FTS

Non-goals in v1: authentication/authorization, UI, multi-tenant billing.

------------------------------------------------------------------------

## 2) High-Level Architecture

-   **Ingestor**: scans Markdown, parses headings/anchors, chunks text,
    generates embeddings via SK, writes chunks to Postgres.
-   **Repository**: Dapper queries/inserts. Hybrid retrieval SQL runs in
    Postgres (pgvector + FTS).
-   **Retriever**: takes a question, computes the query embedding,
    executes hybrid search, returns structured DTOs with citations.
-   **Semantic Kernel Plugins**: expose retriever function to the chat
    model and configure providers (Azure OpenAI first).
-   **Web API**: thin controller returning structured DTOs; no string
    blobs.
-   **Needlr Plugins**: each does exactly one thing.

------------------------------------------------------------------------

## 3) Single-Responsibility Plugins

Each plugin is independently testable and replaceable. No service
registration elsewhere unless strictly necessary.

1.  **PostgresConnectionPlugin**
    -   Responsibility: create `NpgsqlDataSource` with `UseVector()`
        mapping and export `IDbConnection` factory.
    -   Inputs: `DB_CONN` env var.
    -   Outputs: factory for transient scoped connections.
2.  **DapperVectorTypeHandlerPlugin**
    -   Responsibility: register Dapper `TypeHandler<Vector>` once at
        startup.
3.  **SerilogPlugin**
    -   Responsibility: initialize Serilog (console + rolling file).\
    -   Inputs: optional env vars for level and path.
4.  **SemanticKernelAzureOpenAIPlugin**
    -   Responsibility: build and export the SK `Kernel` configured for
        Azure OpenAI chat + embeddings.
    -   Inputs: `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_KEY`,
        `AZURE_OPENAI_CHAT_DEPLOYMENT`, `AZURE_OPENAI_EMBED_DEPLOYMENT`.
5.  **SemanticKernelProviderSelectorPlugin**
    -   Responsibility: read `AI_PROVIDER` and activate the appropriate
        provider plugin (Azure OpenAI default). Foundation for future
        OpenAI/Ollama/etc.
6.  **SchemaMigrationPlugin** (optional in v1 if Docker init SQL is
    used)
    -   Responsibility: run idempotent DDL (create
        extension/table/index/trigger) at startup.
7.  **HealthChecksPlugin** (optional)
    -   Responsibility: register basic liveness/readiness checks for DB
        and embeddings.

> Note: plugin names describe responsibility. Adjust to your Needlr
> plugin discovery conventions.

------------------------------------------------------------------------

## 4) Data Model and Schema

### Table

    lore_chunks (
      id           bigserial PK,
      source_path  text not null,
      anchor_id    text null,
      title        text null,
      headings     text[] null,
      content      text not null,
      tokens       int null,
      word_count   int null,
      links_to     text[] null,
      updated_at   timestamptz not null default now(),
      embedding    vector(768) not null,
      tsv          tsvector null
    )

### Indexes

-   `GIN(tsv)` for FTS
-   `BTREE(source_path)`
-   `HNSW(embedding vector_cosine_ops)`

### Trigger

-   `lore_tsv_update_trg` populates `tsv` from
    `title + headings + content`

> Vector dimension must match the embedding model selected by SK.
> Default 768 is a placeholder.

------------------------------------------------------------------------

## 5) Public Contracts

### DTOs

``` csharp
public sealed record LoreSearchHitDto(
    long Id,
    string SourcePath,
    string? AnchorId,
    string? Title,
    string[]? Headings,
    string Excerpt,
    float DenseScore,
    float? SparseScore,
    float Score
);

public sealed record LoreSearchResponse(
    string Question,
    IReadOnlyList<LoreSearchHitDto> Hits,
    DateTimeOffset GeneratedAtUtc
);
```

### Interfaces

``` csharp
using Pgvector;

public interface ILoreRepository
{
    Task InsertAsync(/* LoreChunk model */);
    Task<IReadOnlyList<LoreSearchHitDto>> HybridSearchAsync(Vector queryVec, string queryText, int k, CancellationToken ct = default);
}

public interface IEmbeddingService
{
    Task<Vector> EmbedAsync(string text, CancellationToken ct = default);
}

public interface ILoreRetriever
{
    Task<LoreSearchResponse> AskAsync(string question, int k = 6, CancellationToken ct = default);
}
```

### SK Function (tool)

-   Class exposes `[KernelFunction] AskAsync(string question)` that
    returns **JSON** of `LoreSearchResponse`. The Web API returns DTOs
    directly.

------------------------------------------------------------------------

## 6) Retrieval Logic

Hybrid search SQL outline:

-   Dense top-N by `embedding <=> :q_vec`
-   Sparse top-M by `ts_rank(tsv, websearch_to_tsquery(:q_text))` with
    `ts_headline` for snippet
-   Join and blend: `0.65 * dense + 0.35 * sparse`
-   Order by blended score, limit k

------------------------------------------------------------------------

## 7) Build Plan and Parallelization

### Workstream A: Infrastructure & Plugins

-   A1 PostgresConnectionPlugin
-   A2 DapperVectorTypeHandlerPlugin
-   A3 SerilogPlugin
-   A4 SemanticKernelAzureOpenAIPlugin
-   A5 SemanticKernelProviderSelectorPlugin
-   A6 SchemaMigrationPlugin (optional if Docker init SQL is
    authoritative)
-   A7 Docker Compose for pgvector and DB init

### Workstream B: Ingestion Pipeline

-   B1 Markdown scanner and front-matter stripper
-   B2 Header-aware chunker with overlap and anchor derivation
-   B3 Embedding generation via `IEmbeddingService` (SK)
-   B4 Batch upsert to DB (Dapper)
-   B5 Idempotency via content hash to avoid unnecessary re-embeds

### Workstream C: Data Access & Retrieval

-   C1 `ILoreRepository` with Dapper insert
-   C2 Hybrid retrieval SQL with FTS snippets
-   C3 `ILoreRetriever` to orchestrate Embed→Query→Map DTOs

### Workstream D: Semantic Kernel Integration

-   D1 `LoreSkFunctions` SK tool that returns JSON (not plain text)
-   D2 Kernel build and provider selection via Needlr plugins
-   D3 Minimal prompt scaffolding for downstream RAG usage

### Workstream E: Web API

-   E1 Controller `GET /api/lore/ask?q=&k=`
-   E2 OpenAPI annotations (optional)
-   E3 Basic request validation

### Workstream F: Observability and Ops

-   F1 Serilog enrichment and filters
-   F2 Structured logs on ingest and query
-   F3 Health endpoints (optional)

> Parallelization: A, B, C, D, E, F can proceed concurrently after
> contracts are stable. A must finish before runtime wiring. B can mock
> `IEmbeddingService`. C can stub `Vector`. D can start with mock
> retriever. E depends only on D and C contracts.

------------------------------------------------------------------------

## 8) To-Do Lists by Workstream

### A) Infrastructure & Plugins

-   [ ] Implement `PostgresConnectionPlugin`: build `NpgsqlDataSource`
    with `.UseVector()` and export `IDbConnection` factory.
-   [ ] Implement `DapperVectorTypeHandlerPlugin`: register
    `SqlMapper.TypeHandler<Vector>`.
-   [ ] Implement `SerilogPlugin`: console + rolling file; configurable
    level.
-   [ ] Implement `SemanticKernelAzureOpenAIPlugin`: add chat +
    embeddings using env vars; export Kernel.
-   [ ] Implement `SemanticKernelProviderSelectorPlugin`: read
    `AI_PROVIDER` and activate provider plugin (default azure-openai).
-   [ ] Implement `SchemaMigrationPlugin` or provide
    `docker-entrypoint-initdb.d` SQL.
-   [ ] Provide `docker-compose.yml` with `pgvector/pgvector:pg16` and
    mounted init SQL.

**Exit Criteria** - Kernel is resolvable from DI. - `IDbConnection` can
be resolved and `SELECT 1` succeeds. - Logs appear in console and
`logs/app-*.log`. - DB has `vector` extension; table/indexes exist.

### B) Ingestion

-   [ ] File walker for `.md`.
-   [ ] Markdown normalizer: strip front-matter, normalize links.
-   [ ] Chunker: split by headings, 300--600 token target, 50--100 token
    overlap, preserve code/list integrity.
-   [ ] Anchor generator from headings.
-   [ ] Tokens/word count computation.
-   [ ] Embedding via `IEmbeddingService`.
-   [ ] Hash-based idempotency; only re-embed changed chunks.
-   [ ] Insert via `ILoreRepository.InsertAsync`.

**Exit Criteria** - Running ingestor on a sample repo loads N\>100
chunks without error. - Re-run with no changes performs zero
re-embeds/inserts. - Average ingest throughput and logs recorded.

### C) Repository & Retrieval

-   [ ] Implement insert with Dapper; verify array and vector mappings.
-   [ ] Implement hybrid SQL; test snippet via `ts_headline`.
-   [ ] Map rows to `LoreSearchHitDto` precisely; no null ref issues.
-   [ ] Implement `ILoreRetriever.AskAsync` returning
    `LoreSearchResponse`.

**Exit Criteria** - Query returns k hits with nonempty excerpts for
known queries. - Scores are present; blended ordering stable across
runs. - p95 latency within target on sample data.

### D) Semantic Kernel

-   [ ] Implement `LoreSkFunctions.AskAsync` `[KernelFunction]`
    returning JSON of `LoreSearchResponse`.
-   [ ] Confirm Kernel can call tool successfully.
-   [ ] Provide minimal system prompt template for downstream RAG.

**Exit Criteria** - Chat model can tool-call `AskAsync` and receive
valid JSON. - JSON deserializes back into `LoreSearchResponse`
losslessly.

### E) Web API

-   [ ] `GET /api/lore/ask?q=&k=6`.
-   [ ] Returns `LoreSearchResponse` DTO, HTTP 200.
-   [ ] Validation for empty `q` and k bounds.

**Exit Criteria** - Curl returns structured JSON with citations:
`source_path#anchor_id`. - Non-happy paths return 400 with clear
message.

### F) Observability

-   [ ] Log each ingest commit and each query with timings and counts.
-   [ ] Optionally register health endpoints.
-   [ ] Redact sensitive keys.

**Exit Criteria** - Logs show elapsed times for embed, SQL, and
end-to-end. - Health endpoint shows DB up and embeddings provider
status.

------------------------------------------------------------------------

## 9) Configuration

Environment variables:

-   `DB_CONN=Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=lore;Pooling=true`
-   `AI_PROVIDER=azure-openai`
-   `AZURE_OPENAI_ENDPOINT=https://<resource>.openai.azure.com/`
-   `AZURE_OPENAI_KEY=...`
-   `AZURE_OPENAI_CHAT_DEPLOYMENT=gpt-4o`\
-   `AZURE_OPENAI_EMBED_DEPLOYMENT=text-embedding-3-large`
-   `LOG_LEVEL=Information`\
-   `LOG_PATH=logs/app-.log`

Vector dimension in schema must match the embedding deployment.

------------------------------------------------------------------------

## 10) Performance Targets

-   Ingest throughput: ≥ 20 chunks/sec on a modest CPU using hosted
    embeddings; local models may differ.
-   Retrieval latency (p95): ≤ 120 ms for top-8 results on 50k chunks
    after warmup.
-   Index build: HNSW creation completes without OOM; tune `ef_search`
    and memory settings separately.

------------------------------------------------------------------------

## 11) Risks and Mitigations

-   **Mismatch of vector dimension**: enforce a startup check comparing
    table vector(dim) vs embedding model dimension.
-   **FTS language mismatch**: make `tsvector` config configurable;
    default to `english`.
-   **Large excerpts**: use `ts_headline` with limits; clamp server
    payload.
-   **Re-embedding cost**: use content hash and file mtime to avoid
    churn.
-   **Provider drift**: provider selector plugin isolates SK config; add
    new providers safely.

------------------------------------------------------------------------

## 12) End-to-End Acceptance Test

1.  Start DB
    -   Run `docker compose up -d`.\
    -   Verify `CREATE EXTENSION vector` and table/indexes exist.
2.  Ingest Sample
    -   Place 10 Markdown files in `./samples/wiki`.
    -   Run ingestor once; expect ≥ 30 chunks inserted.
    -   Re-run; expect 0 new embeddings.
3.  Query API
    -   `GET /api/lore/ask?q=worldstone origin&k=6`
    -   Expect 200 with `LoreSearchResponse` JSON, ≥ 1 hit, each with
        `SourcePath` and `Excerpt`.
4.  SK Tool Call
    -   Run a small console that invokes the Kernel and calls
        `LoreSkFunctions.AskAsync`.
    -   Confirm valid JSON output and round-trip deserialization.
5.  Logs
    -   Confirm Serilog outputs ingestion counts and query timings.

**Exit Criteria** - All steps pass without manual intervention. -
Results contain meaningful excerpts with citations.

------------------------------------------------------------------------

## 13) Implementation Hints

-   Use `NpgsqlDataSourceBuilder(conn).UseVector()` in the Postgres
    plugin to register pgvector types.
-   Register Dapper `TypeHandler<Vector>` once in the Dapper plugin.
-   Use
    `ts_headline('english', content, websearch_to_tsquery('english', @t), 'MaxFragments=1, MinWords=15, MaxWords=40')`
    for stable snippets.
-   Default blend weights `0.65 dense / 0.35 sparse`. Make weights
    configurable later.
-   For chunking, avoid splitting lists/code blocks; keep 50--100 token
    overlap.

------------------------------------------------------------------------

## 14) Deliverables Checklist

-   [ ] `docker-compose.yml` and init SQL
-   [ ] Plugins: PostgresConnection, DapperVectorTypeHandler, Serilog,
    SemanticKernelAzureOpenAI, SemanticKernelProviderSelector,
    SchemaMigration (optional)
-   [ ] Core: `ILoreRepository`, `ILoreRetriever`, `IEmbeddingService`,
    `LoreSkFunctions`
-   [ ] DTOs: `LoreSearchHitDto`, `LoreSearchResponse`
-   [ ] Ingestor: Markdown scanner, chunker, hasher, writer
-   [ ] Web API: `/api/lore/ask`
-   [ ] Basic README with env vars and quickstart

------------------------------------------------------------------------

## 15) Definition of Done

-   All plugins load successfully; configuration via env vars only.
-   Ingesting a real wiki produces stable, deduplicated rows.
-   API returns structured results with excerpts and citations under the
    p95 latency target.
-   SK tool reliably returns JSON for the same results.
-   Logs are sufficiently detailed to troubleshoot ingest and query
    performance.

------------------------------------------------------------------------

## 16) Parallel Build Order for Agents

-   Agent-Infra: deliver A1--A5 and Docker in parallel; provide a stub
    SK if needed.
-   Agent-Data: deliver schema SQL and Repository with stub vector.
-   Agent-Embed: deliver `IEmbeddingService` using Kernel; can mock
    until Infra done.
-   Agent-Ingest: deliver chunker + ingestor using mocked embedding;
    switch to real later.
-   Agent-Retrieval: deliver hybrid SQL and `ILoreRetriever`; unit test
    without API.
-   Agent-API: deliver controller; integration test with Repository and
    Retriever.
-   Agent-Obs: wire Serilog, add timing scopes and basic health probes.
