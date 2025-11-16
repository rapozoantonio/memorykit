# MemoryKit Cognitive Model

## Introduction

MemoryKit's architecture is inspired by cognitive neuroscience research on human memory systems. This document explains the cognitive model and how it maps to the technical implementation.

## Human Memory Systems

### 1. Working Memory (Short-Term Memory)

**Cognitive Function:**
- Temporary storage of active information
- Limited capacity (7±2 items, Miller's Law)
- Fast access and manipulation
- Decays rapidly without rehearsal

**MemoryKit Implementation:**
- **Technology**: Redis (in-memory cache)
- **Storage**: Last N messages in active conversation
- **Capacity**: Configurable (default: 20 messages)
- **TTL**: Session-based, with sliding expiration
- **Use Case**: Current conversation context

**Key Operations:**
```csharp
// Add to working memory
await workingMemory.AddAsync(message);

// Retrieve recent context
var context = await workingMemory.GetRecentAsync(count: 10);
```

### 2. Episodic Memory (Long-Term Memory)

**Cognitive Function:**
- Storage of personal experiences and events
- Temporal and spatial context
- Rich sensory details
- "Mental time travel"

**MemoryKit Implementation:**
- **Technology**: Azure Blob Storage + AI Search
- **Storage**: Complete conversation episodes with metadata
- **Indexing**: Vector embeddings for semantic search
- **Retrieval**: Temporal and similarity-based queries
- **Use Case**: Historical conversation retrieval

**Key Operations:**
```csharp
// Store episode
await episodicMemory.StoreAsync(conversation);

// Query by time
var episodes = await episodicMemory.QueryByTimeAsync(startDate, endDate);

// Semantic search
var similar = await episodicMemory.SearchSimilarAsync(query, topK: 5);
```

### 3. Semantic Memory (Long-Term Memory)

**Cognitive Function:**
- General knowledge and facts
- Decontextualized information
- Concepts and relationships
- Language and vocabulary

**MemoryKit Implementation:**
- **Technology**: Azure AI Search
- **Storage**: Extracted facts and entities
- **Extraction**: AI-powered entity recognition
- **Organization**: Knowledge graph structure
- **Use Case**: Fact retrieval and reasoning

**Key Operations:**
```csharp
// Extract facts
var facts = await semanticMemory.ExtractFactsAsync(text);

// Query knowledge
var knowledge = await semanticMemory.QueryAsync("capital of France");

// Build relationships
await semanticMemory.LinkEntitiesAsync(entity1, entity2, relationship);
```

### 4. Procedural Memory (Long-Term Memory)

**Cognitive Function:**
- Skills and habits
- "How to" knowledge
- Automatic behaviors
- Learned patterns

**MemoryKit Implementation:**
- **Technology**: Azure Table Storage
- **Storage**: Identified patterns and workflows
- **Learning**: Pattern recognition from usage
- **Activation**: Context-triggered suggestions
- **Use Case**: Learned user behaviors and preferences

**Key Operations:**
```csharp
// Record pattern
await proceduralMemory.RecordPatternAsync(pattern);

// Get suggestions
var suggestions = await proceduralMemory.GetSuggestionsAsync(context);

// Update confidence
await proceduralMemory.UpdateConfidenceAsync(patternId, success);
```

## Cognitive Components

### The Amygdala (Importance Engine)

**Cognitive Function:**
- Emotional significance assessment
- Attention allocation
- Memory consolidation trigger

**MemoryKit Implementation:**
```csharp
public class AmygdalaImportanceEngine : IImportanceEngine
{
    public async Task<double> CalculateImportanceAsync(Message message)
    {
        // Analyze multiple factors:
        // - Emotional content
        // - Novelty
        // - User engagement signals
        // - Topic relevance
        
        return importanceScore; // 0.0 to 1.0
    }
}
```

**Importance Factors:**
1. **Emotional Intensity**: Sentiment analysis
2. **Novelty**: Information gain
3. **Relevance**: Topic alignment
4. **User Signals**: Explicit feedback
5. **Repetition**: Frequency of mention

### The Hippocampus (Memory Indexer)

**Cognitive Function:**
- Memory consolidation
- Pattern completion
- Spatial and temporal binding

**MemoryKit Implementation:**
```csharp
public class HippocampusIndexer : IMemoryIndexer
{
    public async Task ConsolidateAsync(Conversation conversation)
    {
        // 1. Extract key information
        var facts = await ExtractFactsAsync(conversation);
        
        // 2. Create embeddings
        var embeddings = await CreateEmbeddingsAsync(facts);
        
        // 3. Store in episodic memory
        await StoreEpisodicAsync(conversation, embeddings);
        
        // 4. Update semantic memory
        await UpdateSemanticAsync(facts);
        
        // 5. Identify patterns
        await UpdateProceduralAsync(conversation);
    }
}
```

### The Prefrontal Cortex (Executive Controller)

**Cognitive Function:**
- Attention control
- Query planning
- Memory retrieval strategy
- Working memory management

**MemoryKit Implementation:**
```csharp
public class PrefrontalController : IQueryPlanner
{
    public async Task<QueryPlan> PlanQueryAsync(string query, QueryContext context)
    {
        // 1. Classify query type
        var queryType = await ClassifyQueryAsync(query);
        
        // 2. Determine memory layers to search
        var layers = DetermineMemoryLayers(queryType);
        
        // 3. Plan retrieval strategy
        var strategy = CreateRetrievalStrategy(queryType, layers);
        
        // 4. Allocate resources
        var plan = new QueryPlan
        {
            Type = queryType,
            Layers = layers,
            Strategy = strategy,
            MaxResults = CalculateMaxResults(context)
        };
        
        return plan;
    }
}
```

## Memory Consolidation Process

Similar to sleep-dependent memory consolidation in humans:

### 1. Acquisition Phase
- Messages enter working memory
- Importance scores calculated
- Immediate context maintained

### 2. Consolidation Phase (Background)
```csharp
public async Task ConsolidateAsync()
{
    // 1. Identify high-importance items
    var important = await workingMemory
        .GetByImportanceAsync(threshold: 0.7);
    
    // 2. Extract facts
    var facts = await ExtractFactsAsync(important);
    
    // 3. Store in episodic memory
    await episodicMemory.StoreAsync(important);
    
    // 4. Update semantic memory
    await semanticMemory.AddFactsAsync(facts);
    
    // 5. Identify patterns
    var patterns = await IdentifyPatternsAsync(important);
    await proceduralMemory.UpdatePatternsAsync(patterns);
    
    // 6. Prune working memory
    await workingMemory.PruneOldItemsAsync();
}
```

### 3. Retrieval Phase
- Multi-layer search
- Relevance scoring
- Context integration

## Query Classification

Different query types activate different memory systems:

### Factual Queries
- **Example**: "What is Paris?"
- **Memory**: Semantic
- **Strategy**: Direct fact lookup

### Episodic Queries
- **Example**: "What did we discuss yesterday?"
- **Memory**: Episodic
- **Strategy**: Temporal + semantic search

### Procedural Queries
- **Example**: "How do I usually do this?"
- **Memory**: Procedural
- **Strategy**: Pattern matching

### Contextual Queries
- **Example**: "Tell me more about that"
- **Memory**: Working + Episodic
- **Strategy**: Context resolution + semantic search

## Forgetting and Memory Decay

### Natural Decay
```csharp
public class MemoryDecayPolicy
{
    public async Task ApplyDecayAsync()
    {
        // Working memory: Fast decay
        await workingMemory.ApplyDecayAsync(
            halfLife: TimeSpan.FromHours(1)
        );
        
        // Episodic memory: Slow decay based on access
        await episodicMemory.ApplyDecayAsync(
            accessWeight: 0.8,
            ageWeight: 0.2
        );
    }
}
```

### Selective Forgetting
- Low-importance items removed first
- Redundant information consolidated
- Error pruning for learned patterns

## Attention Mechanisms

### Selective Attention
```csharp
public class AttentionMechanism
{
    public async Task<IEnumerable<Memory>> ApplyAttentionAsync(
        IEnumerable<Memory> memories,
        QueryContext context)
    {
        return memories
            .OrderByDescending(m => CalculateAttentionScore(m, context))
            .Take(context.MaxResults);
    }
    
    private double CalculateAttentionScore(Memory memory, QueryContext context)
    {
        // Factors:
        // - Recency
        // - Relevance to current query
        // - Importance score
        // - Access frequency
        // - Emotional salience
        
        return score;
    }
}
```

## Integration with AI Models

### Embedding Generation
```csharp
// Semantic Kernel integration
var embedding = await semanticKernel.GetEmbeddingAsync(text);
await memoryStore.StoreEmbeddingAsync(embedding, metadata);
```

### Fact Extraction
```csharp
// Entity extraction
var entities = await semanticKernel.ExtractEntitiesAsync(text);
await semanticMemory.StoreEntitiesAsync(entities);
```

### Pattern Learning
```csharp
// Behavior analysis
var patterns = await aiModel.AnalyzePatternAsync(
    conversationHistory,
    userBehavior
);
await proceduralMemory.UpdatePatternsAsync(patterns);
```

## Performance Characteristics

### Access Times (typical)
- **Working Memory**: <10ms (Redis)
- **Episodic Memory**: 50-200ms (AI Search)
- **Semantic Memory**: 20-100ms (AI Search)
- **Procedural Memory**: <50ms (Table Storage)

### Capacity
- **Working Memory**: ~1MB per session
- **Episodic Memory**: Unlimited (blob storage)
- **Semantic Memory**: Millions of facts
- **Procedural Memory**: Thousands of patterns

## Future Enhancements

1. **Reconsolidation**: Update memories upon retrieval
2. **Meta-memory**: Memory about memories
3. **Schema Theory**: Template-based memory organization
4. **Spreading Activation**: Network-based retrieval
5. **Autobiographical Memory**: User-specific memory organization

## References

This cognitive model is inspired by:

- Atkinson-Shiffrin Multi-Store Model (1968)
- Baddeley's Working Memory Model (1974)
- Tulving's Memory Systems Theory (1985)
- ACT-R Cognitive Architecture (1993)
- Modern neuroscience research on memory consolidation

## Conclusion

MemoryKit bridges cognitive science and software engineering, creating an AI memory system that behaves like human memory while leveraging modern cloud infrastructure for scalability and reliability.
