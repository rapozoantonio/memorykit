-- MemoryKit SQLite Schema
-- Optimized for local deployment without Vector extensions or JSONB
-- Note: SQLite uses BLOB for embeddings, TEXT for JSON metadata

-- Working Memory: Short-term, active (<5ms access)
-- Stores immediate conversation context and temporary information
CREATE TABLE working_memories (
    id TEXT PRIMARY KEY,
    conversation_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    content TEXT NOT NULL,
    importance REAL NOT NULL DEFAULT 0.5,
    created_at TEXT NOT NULL,
    expires_at TEXT,
    promoted_to TEXT
);
CREATE INDEX idx_working_conv_created ON working_memories(conversation_id, created_at DESC);

-- Semantic Memory: Facts (<50ms access)
-- Stores knowledge, facts, and information (no vector search in SQLite, use text fallback)
CREATE TABLE semantic_facts (
    id TEXT PRIMARY KEY,
    conversation_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    content TEXT NOT NULL,
    fact_type TEXT,
    confidence REAL DEFAULT 0.95,
    embedding BLOB,
    metadata TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX idx_semantic_conv ON semantic_facts(conversation_id);

-- Episodic Memory: Events, temporal (<100ms access)
-- Stores time-based events and interactions with temporal decay
CREATE TABLE episodic_events (
    id TEXT PRIMARY KEY,
    conversation_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    event_type TEXT,
    content TEXT NOT NULL,
    participants TEXT,
    occurred_at TEXT NOT NULL,
    decay_factor REAL DEFAULT 1.0,
    metadata TEXT,
    created_at TEXT NOT NULL
);
CREATE INDEX idx_episodic_conv_occurred ON episodic_events(conversation_id, occurred_at DESC);

-- Procedural Memory: Patterns, permanent (<200ms access)
-- Stores learned behaviors, patterns, and procedures with success metrics
CREATE TABLE procedural_patterns (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    pattern_name TEXT NOT NULL,
    trigger_conditions TEXT NOT NULL,
    learned_response TEXT NOT NULL,
    success_count INTEGER DEFAULT 0,
    failure_count INTEGER DEFAULT 0,
    last_used_at TEXT,
    metadata TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX idx_procedural_user ON procedural_patterns(user_id);
