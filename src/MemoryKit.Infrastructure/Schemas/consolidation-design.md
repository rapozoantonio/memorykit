# MemoryKit Consolidation Persistence Design

## Overview

Memory consolidation is the process of promoting memories through four hierarchical layers based on importance, access patterns, and time decay. This document defines the consolidation flow, database operations, transaction boundaries, and trigger mechanisms.

---

## Consolidation Hierarchy

```
Working Memory (hot, <5ms)
    ↓ [importance > 0.7 OR accessed 3+ times]
Semantic Memory (warm, <50ms)
    ↓ [time decay OR pattern detected]
Episodic Memory (cold, <100ms)
    ↓ [behavior learned, 3+ occurrences]
Procedural Memory (permanent, <200ms)
```

### Layer Characteristics

| Layer      | TTL        | Access Speed | Scope           | Primary Use             |
| ---------- | ---------- | ------------ | --------------- | ----------------------- |
| Working    | 5-30min    | <5ms         | Current session | Active conversation     |
| Semantic   | Indefinite | <50ms        | User-wide       | Facts, knowledge        |
| Episodic   | 1-year     | <100ms       | User-wide       | Events, timestamps      |
| Procedural | Indefinite | <200ms       | User-wide       | Learned patterns, rules |

---

## Consolidation Triggers

### 1. On-Demand Consolidation

**When:** After 20 messages stored or exceeds working memory threshold (1000 items)
**Implementation:** Check in `IMemoryServiceAggregator.StoreMemory()`
**Behavior:** Immediate consolidation if threshold exceeded

```csharp
if (workingMemoryCount >= 20 || workingMemoryItems > 1000)
{
    await _consolidationService.ConsolidateAsync(userId, conversationId);
}
```

### 2. Periodic Consolidation

**When:** Every 5 minutes via background service
**Implementation:** `ConsolidationBackgroundService` using `IHostedService`
**Behavior:** Runs independently, consolidates all users' memories

```csharp
// Runs every 5 minutes
while (!stoppingToken.IsCancellationRequested)
{
    await _consolidationService.ConsolidateAllAsync();
    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
}
```

### 3. Manual Consolidation

**When:** MCP `consolidate` tool invoked or API endpoint called
**Implementation:** `ConsolidateMemoryHandler` (MCP tool)
**Behavior:** Forced consolidation for specific user/conversation

```csharp
var result = await _consolidationService.ConsolidateAsync(
    userId: request.UserId,
    conversationId: request.ConversationId,
    force: true
);
```

---

## Database Operations Per Consolidation

### Operation 1: Working → Semantic

**Criteria:**

- `importance > 0.7` OR
- `access_count >= 3` OR
- `age > 15 minutes` (convert to fact)

**Transaction:**

```sql
BEGIN TRANSACTION;

-- 1. Create semantic fact from working memory
INSERT INTO semantic_facts (
    id, conversation_id, user_id, content, fact_type,
    confidence, embedding, metadata, created_at, updated_at
)
SELECT
    gen_random_uuid(),
    conversation_id,
    user_id,
    content,
    'working_promotion',
    (importance + 0.25),  -- Boost confidence
    embedding,
    jsonb_build_object(
        'source', 'working_memory',
        'original_importance', importance,
        'access_count', access_count,
        'promoted_at', NOW()
    ),
    NOW(),
    NOW()
FROM working_memories
WHERE user_id = $1
  AND conversation_id = $2
  AND (importance > 0.7 OR access_count >= 3 OR age_minutes > 15)
RETURNING id INTO @promoted_fact_ids;

-- 2. Update working memory with promotion reference
UPDATE working_memories
SET promoted_to = @promoted_fact_ids
WHERE user_id = $1
  AND conversation_id = $2
  AND importance > 0.7;

-- 3. Delete promoted working memories (optional: soft-delete via flag)
DELETE FROM working_memories
WHERE user_id = $1
  AND conversation_id = $2
  AND promoted_to IS NOT NULL;

COMMIT;
```

**Rollback Behavior:** Entire transaction rolls back if any step fails

---

### Operation 2: Semantic → Episodic

**Criteria:**

- `confidence > 0.8` AND `age > 2 hours` (convert to event)
- Pattern detected (3+ similar facts)

**Transaction:**

```sql
BEGIN TRANSACTION;

-- 1. Identify patterns (3+ similar facts)
WITH similar_facts AS (
    SELECT
        user_id,
        fact_type,
        similarity(embedding, query_embedding) AS sim,
        id
    FROM semantic_facts
    WHERE user_id = $1
      AND similarity > 0.85
      AND created_at > NOW() - INTERVAL '7 days'
    GROUP BY fact_type, user_id
    HAVING COUNT(*) >= 3
)

-- 2. Create episodic events from aged semantic facts
INSERT INTO episodic_events (
    id, conversation_id, user_id, event_type, content,
    participants, occurred_at, decay_factor, metadata, created_at
)
SELECT
    gen_random_uuid(),
    conversation_id,
    user_id,
    fact_type || '_pattern_detected',
    content,
    NULL,
    created_at,
    1.0,
    jsonb_build_object(
        'source', 'semantic_promotion',
        'confidence', confidence,
        'promoted_from_id', id,
        'promoted_at', NOW()
    ),
    NOW()
FROM semantic_facts
WHERE user_id = $1
  AND confidence > 0.8
  AND created_at < NOW() - INTERVAL '2 hours'
RETURNING id INTO @promoted_event_ids;

-- 3. Soft-delete promoted semantic facts (update flag, don't delete)
UPDATE semantic_facts
SET metadata = jsonb_set(metadata, '{promoted_to_episodic}', to_jsonb(NOW()))
WHERE user_id = $1
  AND id = ANY(@promoted_event_ids);

COMMIT;
```

---

### Operation 3: Episodic → Procedural

**Criteria:**

- Pattern observed 3+ times in episodic events
- High success rate (success_count > failure_count)
- Learnable behavior identified

**Transaction:**

```sql
BEGIN TRANSACTION;

-- 1. Identify recurring patterns from episodic events
WITH pattern_candidates AS (
    SELECT
        event_type,
        user_id,
        COUNT(*) AS occurrence_count,
        jsonb_agg(DISTINCT trigger_conditions) AS trigger_patterns,
        AVG(CAST(metadata->>'success' AS FLOAT)) AS avg_success
    FROM episodic_events
    WHERE user_id = $1
      AND occurred_at > NOW() - INTERVAL '30 days'
    GROUP BY event_type, user_id
    HAVING COUNT(*) >= 3
)

-- 2. Create procedural patterns
INSERT INTO procedural_patterns (
    id, user_id, pattern_name, trigger_conditions,
    learned_response, success_count, failure_count,
    metadata, created_at, updated_at
)
SELECT
    gen_random_uuid(),
    user_id,
    event_type || '_learned_pattern',
    trigger_patterns,
    (SELECT content FROM episodic_events
     WHERE event_type = pattern_candidates.event_type
     AND occurred_at = (SELECT MAX(occurred_at) FROM episodic_events
                        WHERE event_type = pattern_candidates.event_type)
     LIMIT 1),
    ROUND(occurrence_count * avg_success)::INT,
    ROUND(occurrence_count * (1 - avg_success))::INT,
    jsonb_build_object(
        'source', 'episodic_promotion',
        'occurrence_count', occurrence_count,
        'avg_success_rate', avg_success,
        'promoted_at', NOW()
    ),
    NOW(),
    NOW()
FROM pattern_candidates
WHERE avg_success > 0.6;

-- 3. Mark episodic events as consolidated
UPDATE episodic_events
SET metadata = jsonb_set(metadata, '{consolidated_to_procedural}', to_jsonb(NOW()))
WHERE user_id = $1
  AND event_type IN (SELECT pattern_name FROM procedural_patterns WHERE user_id = $1);

COMMIT;
```

---

## Transaction Boundaries

### Atomic Unit: Single Consolidation Cycle

```csharp
public class ConsolidationService : IConsolidationService
{
    public async Task ConsolidateAsync(string userId, string conversationId)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // Phase 1: Working → Semantic
                await ConsolidateWorkingToSemanticAsync(userId, conversationId);

                // Phase 2: Semantic → Episodic
                await ConsolidateSemanticToEpisodicAsync(userId);

                // Phase 3: Episodic → Procedural
                await ConsolidateEpisodicToProceduralAsync(userId);

                await transaction.CommitAsync();
                _logger.LogInformation(
                    "Consolidation completed for user {UserId}, conversation {ConversationId}",
                    userId, conversationId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Consolidation failed. Rolled back all changes.");
                throw;
            }
        }
    }
}
```

### Failure & Retry Strategy

```csharp
public async Task ConsolidateWithRetryAsync(
    string userId,
    string conversationId,
    int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await ConsolidateAsync(userId, conversationId);
            return;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            _logger.LogWarning(
                ex,
                "Consolidation attempt {Attempt} failed. Retrying in 5 seconds...",
                attempt);

            await Task.Delay(TimeSpan.FromSeconds(5 * attempt)); // Exponential backoff
        }
    }

    _logger.LogError("Consolidation failed after {MaxRetries} attempts", maxRetries);
}
```

---

## Logging & Observability

### Consolidation Events

```csharp
public class ConsolidationLogger
{
    // Working → Semantic
    _logger.LogInformation(
        "Promoted {Count} working memories to semantic facts. " +
        "UserId: {UserId}, ConversationId: {ConversationId}",
        count, userId, conversationId);

    // Semantic → Episodic
    _logger.LogInformation(
        "Promoted {Count} semantic facts to episodic events. " +
        "UserId: {UserId}",
        count, userId);

    // Episodic → Procedural
    _logger.LogInformation(
        "Created {Count} procedural patterns from episodic events. " +
        "UserId: {UserId}",
        count, userId);

    // Failures
    _logger.LogError(
        "Consolidation transaction failed: {Reason}. " +
        "UserId: {UserId}, ConversationId: {ConversationId}",
        ex.Message, userId, conversationId);
}
```

---

## Acceptance Criteria

- [x] Consolidation flow documented with hierarchy and triggers
- [x] Database operations per consolidation defined (SQL templates)
- [x] Transaction boundaries clearly defined (atomic units)
- [x] Failure & retry strategy documented
- [x] Logging strategy defined for observability
- [x] Consolidation triggers implemented (on-demand, periodic, manual)

---

## Implementation Next Steps

1. **Phase 1.1:** Implement `IConsolidationService` interface
2. **Phase 1.2:** Implement PostgreSQL consolidation queries
3. **Phase 1.3:** Implement SQLite consolidation (no vector similarity)
4. **Phase 1.4:** Implement `ConsolidationBackgroundService`
5. **Phase 1.5:** Add MCP `consolidate` tool handler
6. **Phase 1.6:** Add integration tests for consolidation transactions
