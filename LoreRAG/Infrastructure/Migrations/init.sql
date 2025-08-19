-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Create the main lore_chunks table
CREATE TABLE IF NOT EXISTS lore_chunks (
    id BIGSERIAL PRIMARY KEY,
    source_path TEXT NOT NULL,
    anchor_id TEXT NULL,
    title TEXT NULL,
    headings TEXT[] NULL,
    content TEXT NOT NULL,
    tokens INT NULL,
    word_count INT NULL,
    links_to TEXT[] NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    embedding vector(768) NOT NULL,
    tsv tsvector NULL
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_lore_chunks_tsv ON lore_chunks USING GIN (tsv);
CREATE INDEX IF NOT EXISTS idx_lore_chunks_source_path ON lore_chunks USING BTREE (source_path);
CREATE INDEX IF NOT EXISTS idx_lore_chunks_embedding ON lore_chunks USING hnsw (embedding vector_cosine_ops);

-- Create trigger function to update tsvector
CREATE OR REPLACE FUNCTION lore_tsv_update_trigger() RETURNS trigger AS $$
BEGIN
    NEW.tsv = to_tsvector('english',
        COALESCE(NEW.title, '') || ' ' ||
        COALESCE(array_to_string(NEW.headings, ' '), '') || ' ' ||
        COALESCE(NEW.content, '')
    );
    RETURN NEW;
END
$$ LANGUAGE plpgsql;

-- Create trigger to automatically update tsvector
DROP TRIGGER IF EXISTS lore_tsv_update_trg ON lore_chunks;
CREATE TRIGGER lore_tsv_update_trg
BEFORE INSERT OR UPDATE ON lore_chunks
FOR EACH ROW EXECUTE FUNCTION lore_tsv_update_trigger();