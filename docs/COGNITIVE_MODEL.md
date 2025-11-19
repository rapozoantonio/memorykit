# MemoryKit Cognitive Model

## Overview

MemoryKit models human cognition to create efficient memory systems for LLM applications. This document explains the neuroscience-inspired architecture.

## Brain Component Mapping

### 1. Prefrontal Cortex → PrefrontalController

**Role**: Executive function and attention control

The prefrontal cortex:
- Makes decisions about what to focus on
- Plans complex behaviors
- Evaluates alternatives
- Manages working memory

**Software Implementation**:
```csharp
public interface IPrefrontalController
{
    Task<QueryPlan> BuildQueryPlanAsync(string query, ConversationState state);
    Task<QueryType> ClassifyQueryAsync(string query);
    List<MemoryLayer> DetermineLayersToUse(QueryType type, ConversationState state);
}
```

**Query Planning Strategy**:
- Continuation → Layer 3 only (fast)
- Fact Retrieval → Layers 2-3 (balanced)
- Deep Recall → Layers 1-3 (thorough)
- Complex → All layers (comprehensive)
- Procedural → Layers 3 + P (routine)

### 2. Amygdala → AmygdalaImportanceEngine

**Role**: Emotional tagging and importance scoring

The amygdala:
- Tags experiences with emotional significance
- Influences what gets consolidated to long-term memory
- Modulates memory strength based on emotional arousal

**Software Implementation**:
```csharp
public interface IAmygdalaImportanceEngine : IImportanceEngine
{
    Task<double> CalculateImportanceAsync(Message message);
    Task<(double Score, string Sentiment)> AnalyzeSentimentAsync(string text);
    bool ContainsDecisionLanguage(string text);
    bool HasExplicitImportanceMarkers(string text);
}
```

**Importance Scoring Algorithm**:
```
FinalScore = (BaseScore × 0.4) + 
             (EmotionalWeight × 0.3) + 
             (NoveltyBoost × 0.2) + 
             (RecencyFactor × 0.1)
```

**Importance Triggers**:
- User questions (1.5x boost)
- Decisions/commitments (2.0x boost)
- Emotional language (1.3x boost)
- Novel information (1.5x boost)
- Explicit importance markers (2.5x boost)

### 3. Hippocampus → HippocampusIndexer

**Role**: Temporary storage and consolidation initiation

The hippocampus:
- Rapidly encodes new information
- Indexes memories for later retrieval
- Initiates consolidation to cortical storage

**Software Implementation**:
```csharp
public interface IHippocampusIndexer
{
    Task<string> EncodeAsync(Message message);
    Task MarkForConsolidationAsync(string messageId);
    Task ConsolidateAsync(string userId);
}
```

**Consolidation Process**:
1. Encoding: Initial capture in working memory
2. Indexing: Create search indices
3. Importance Scoring: Amygdala processing
4. Consolidation: Move to appropriate layer
   - High importance → Layer 1 (episodic)
   - Facts → Layer 2 (semantic)
   - Patterns → Layer P (procedural)

### 4. Basal Ganglia → ProceduralMemoryService

**Role**: Procedural memory and habitual responses

The basal ganglia:
- Store learned procedures and routines
- Execute habitual behaviors
- Learn from reward and feedback

**Software Implementation**:
```csharp
public interface IProceduralMemoryService
{
    Task<ProceduralPattern?> MatchPatternAsync(string userId, string query);
    Task DetectAndStorePatternAsync(string userId, Message message);
    Task<ProceduralPattern[]> GetUserPatternsAsync(string userId);
}
```

**Pattern Learning**:
- Detect: Extract rules from user instructions
- Store: Save as procedural patterns
- Reinforce: Increase confidence with each use
- Decay: Reduce confidence if unused

### 5. Neocortex → ScratchpadService

**Role**: Long-term semantic knowledge

The neocortex:
- Stores consolidated semantic knowledge
- Integrates information across domains
- Supports reasoning and generalization

**Software Implementation**: Azure Table Storage with semantic indexing

## Memory System

### Working Memory (Layer 3)
**Capacity**: ~7±2 items
**Substrate**: Redis cache
**Latency**: <5ms
**Duration**: ~30 seconds to minutes
**Function**: Active processing

### Semantic Memory (Layer 2)
**Capacity**: Unlimited
**Substrate**: Azure Table Storage + embeddings
**Latency**: ~30ms
**Duration**: Long-term
**Function**: Facts, concepts, relationships

### Episodic Memory (Layer 1)
**Capacity**: Full history
**Substrate**: Azure Blob + AI Search
**Latency**: ~120ms
**Duration**: Long-term
**Function**: Specific events and experiences

### Procedural Memory (Layer P)
**Capacity**: Learned patterns
**Substrate**: Azure Table Storage
**Latency**: ~50ms
**Duration**: Long-term
**Function**: Skills and routines

## Sleep-Based Consolidation

Unlike human sleep, MemoryKit performs continuous consolidation:

```
Event → Working Memory → Importance Scoring → 
Consolidation Decision → Target Layer Storage
```

**Consolidation Rules**:
- High importance + specific facts → Layer 1
- General knowledge → Layer 2
- Repeated procedures → Layer P
- Low importance → Discard after TTL

## Attention and Filtering

**Selective Attention**:
The PrefrontalController manages attention by:
1. Classifying query type
2. Determining relevant layers
3. Filtering irrelevant information
4. Assembling minimal context

**Token Efficiency**:
- Continuation: ~100 tokens
- Fact Retrieval: ~300-500 tokens
- Deep Recall: ~1000-1500 tokens
- Complex: ~2000 tokens

## Learning and Adaptation

### Reinforcement Learning
- Procedural patterns increase confidence with use
- Importance thresholds adjust based on retention
- Layer selection optimizes based on query latency

### Semantic Learning
- Entity embeddings capture relationships
- Fact importance updates as accessed
- Procedural rules strengthen with application

## Emotional Dimension

**Emotional Arousal**: Increases importance
- Explicit markers: "important", "critical"
- Sentiment analysis: high positive/negative
- Decision language: commitments, promises
- Novelty: new information

**Emotional Decay**: Importance fades over time
- Recent events more salient
- Exponential decay function
- Can be reinforced through repeated access

## Psychological Principles

### 1. Spacing Effect
- Repeated access reinforces memories
- Intervals optimize consolidation
- Implemented through access tracking

### 2. Recency Effect
- Recent items prioritized
- Reflected in working memory LRU
- Temporal weighting in retrieval

### 3. Primacy Effect
- First occurrences marked as novel
- Initial importance boost
- Entity tracking captures first mention

### 4. Reconstructive Memory
- Context influences recall
- MemoryContext reassembles information
- Query influences what's retrieved

### 5. Transfer of Learning
- Procedural patterns enable transfer
- Semantic knowledge generalizes
- Relationships captured in embeddings

## Disorders & Failure Modes

### Working Memory Overflow
**Problem**: Too many recent items
**Solution**: LRU eviction, importance-based retention

### Semantic Degradation
**Problem**: False or outdated facts
**Solution**: Versioning, update tracking, confidence scores

### Procedural Rigidity
**Problem**: Patterns too strict, not adapting
**Solution**: Dynamic confidence thresholds, decay for unused patterns

### Consolidation Failure
**Problem**: Important information lost
**Solution**: Redundant storage, importance-based TTL

## Future Enhancements

### 1. Metacognition
- Monitor own performance
- Adjust learning strategies
- Self-aware uncertainty

### 2. Emotional Regulation
- Learn from emotional responses
- Adjust importance scoring over time
- Empathetic responses

### 3. Executive Function Improvement
- Better query planning
- Confidence calibration
- Multi-step reasoning

### 4. Transfer Learning
- Learn across domains
- Apply patterns broadly
- Generalize from experience

## References

### Neuroscience
- Kandel, E.R., et al. "Principles of Neural Science"
- LeDoux, J. "The Emotional Brain"
- Squire, L.R. "Memory and the Hippocampus"

### Cognitive Psychology
- Baddeley, A. "Working Memory: Theories, Models, and Controversies"
- Tulving, E. "Elements of Episodic Memory"
- Anderson, J.R. "Cognitive Psychology and its Implications"

### AI/LLM Applications
- Vaswani, A., et al. "Attention is All You Need"
- Brown, T.B., et al. "Language Models are Few-Shot Learners"
- Wei, J., et al. "Emergent Abilities of Large Language Models"

