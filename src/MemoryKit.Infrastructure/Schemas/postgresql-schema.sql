-- MemoryKit PostgreSQL Schema
-- Enable pgvector extension for vector search
CREATE EXTENSION IF NOT EXISTS vector;

-- Working Memory: Short-term, active (<5ms access)
-- Stores immediate conversation context and temporary information
CREATE TABLE working_memories (
    id UUID PRIMARY KEY,
    conversation_id UUID NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    importance FLOAT NOT NULL DEFAULT 0.5,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP,
    promoted_to UUID REFERENCES semantic_facts(id)
);
CREATE INDEX idx_working_conv_created ON working_memories(conversation_id, created_at DESC);
CREATE INDEX idx_working_expires ON working_memories(expires_at) WHERE expires_at IS NOT NULL;

-- Semantic Memory: Facts (<50ms access)
-- Stores knowledge, facts, and information with vector embeddings for similarity search
CREATE TABLE semantic_facts (
    id UUID PRIMARY KEY,
    conversation_id UUID NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    fact_type VARCHAR(50),
    confidence FLOAT DEFAULT 0.95,
    embedding VECTOR(1536),
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_semantic_conv ON semantic_facts(conversation_id);
CREATE INDEX idx_semantic_embedding ON semantic_facts USING hnsw (embedding vector_cosine_ops);
CREATE INDEX idx_semantic_facttype ON semantic_facts(fact_type);

-- Episodic Memory: Events, temporal (<100ms access)
-- Stores time-based events and interactions with temporal decay
CREATE TABLE episodic_events (
    id UUID PRIMARY KEY,
    conversation_id UUID NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    event_type VARCHAR(50),
    content TEXT NOT NULL,
    participants JSONB,
    occurred_at TIMESTAMP NOT NULL,
    decay_factor FLOAT DEFAULT 1.0,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_episodic_conv_occurred ON episodic_events(conversation_id, occurred_at DESC);
CREATE INDEX idx_episodic_eventtype ON episodic_events(event_type);

-- Procedural Memory: Patterns, permanent (<200ms access)
-- Stores learned behaviors, patterns, and procedures with success metrics
CREATE TABLE procedural_patterns (
    id UUID PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    pattern_name VARCHAR(255) NOT NULL,
    trigger_conditions JSONB NOT NULL,
    learned_response TEXT NOT NULL,
    success_count INT DEFAULT 0,
    failure_count INT DEFAULT 0,
    last_used_at TIMESTAMP,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_procedural_user ON procedural_patterns(user_id);
CREATE INDEX idx_procedural_pattern_name ON procedural_patterns(pattern_name);
CREATE INDEX idx_procedural_conditions ON procedural_patterns USING gin (trigger_conditions);
