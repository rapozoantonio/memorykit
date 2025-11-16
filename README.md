# 🧠 **MemoryKit: Technical Requirements Document**

**Version:** 1.0.0  
**Status:** Implementation Ready  
**Author:** Antonio Rapozo  
**Date:** November 16, 2025  
**License:** MIT  
**Repository:** github.com/antoniorapozo/memorykit  

---

## **Document Control**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2025-11-16 | Antonio Rapozo | Initial release |

---

# **TABLE OF CONTENTS**

1. [Executive Summary](#1-executive-summary)
2. [Problem Statement](#2-problem-statement)
3. [Solution Overview](#3-solution-overview)
4. [System Architecture](#4-system-architecture)
5. [Cognitive Model Mapping](#5-cognitive-model-mapping)
6. [Component Specifications](#6-component-specifications)
7. [Data Models](#7-data-models)
8. [API Specification](#8-api-specification)
9. [Memory Layer Implementations](#9-memory-layer-implementations)
10. [AI/LLM Integration](#10-aillm-integration)
11. [Performance Requirements](#11-performance-requirements)
12. [Security & Compliance](#12-security--compliance)
13. [Testing Strategy](#13-testing-strategy)
14. [Deployment Architecture](#14-deployment-architecture)
15. [Technology Stack](#15-technology-stack)
16. [Development Roadmap](#16-development-roadmap)
17. [Success Metrics](#17-success-metrics)
18. [Appendices](#18-appendices)

---

# **1. EXECUTIVE SUMMARY**

## **1.1 Project Vision**

MemoryKit is an enterprise-grade, neuroscience-inspired memory infrastructure for Large Language Model (LLM) applications, built with .NET 8/9 and designed for Azure-native deployments.

**Unique Value Proposition:**
- **98-99% reduction in token costs** through intelligent memory layering
- **Neuroscience-backed architecture** mapping human cognitive systems
- **First .NET implementation** of procedural memory for LLMs
- **Production-ready** with clean architecture and Azure integration

## **1.2 Target Audience**

- Enterprise developers building LLM-powered applications
- .NET architects implementing AI solutions
- Startups requiring cost-effective memory at scale
- Open-source contributors interested in cognitive AI

## **1.3 Key Differentiators**

| Feature | MemoryKit | Competitors |
|---------|-----------|-------------|
| Language | .NET 8/9 | Python (Mem0, Letta) |
| Architecture | Clean Architecture | Monolithic |
| Procedural Memory | ✅ Built-in | ❌ Not available |
| Cloud Integration | Azure-native | Generic |
| Cost Optimization | 99% reduction | 80-90% reduction |
| Emotional Weighting | ✅ Amygdala analog | ❌ Basic scoring |
| Enterprise Ready | ✅ Day 1 | Requires hardening |

## **1.4 Success Criteria**

- ✅ **Performance:** Sub-150ms query response time
- ✅ **Cost:** <$0.10 per 1,000 conversations
- ✅ **Scale:** Handle 10,000+ concurrent conversations
- ✅ **Quality:** 95%+ relevant context retrieval accuracy
- ✅ **Adoption:** 100+ GitHub stars in first 3 months

---

# **2. PROBLEM STATEMENT**

## **2.1 The Goldfish Problem**

Modern LLMs face critical limitations:

```
┌─────────────────────────────────────────────────────┐
│  Current LLM Applications                           │
├─────────────────────────────────────────────────────┤
│  ❌ No persistent memory across sessions            │
│  ❌ Require full context in every prompt            │
│  ❌ Expensive: $50+ per long conversation           │
│  ❌ Cannot learn user preferences/workflows         │
│  ❌ Repetitive instructions needed                  │
│  ❌ No procedural memory (routines)                 │
└─────────────────────────────────────────────────────┘
```

## **2.2 Market Context**

**Existing Solutions:**

| Solution | Language | Strengths | Weaknesses |
|----------|----------|-----------|------------|
| **Mem0** | Python | Simple API | No procedural memory, Python-only |
| **Letta AI** | Python | Agent-focused | Complex, research-grade |
| **OpenAI Memory** | Closed API | Integrated | Black box, vendor lock-in |
| **LangChain Memory** | Python | Ecosystem | Not cognitive-inspired |

**Gap:** No enterprise .NET solution with neuroscience-backed architecture.

## **2.3 Business Impact**

For a typical enterprise chatbot handling 10,000 conversations/month:

| Approach | Token Cost | Infrastructure | Total/Month |
|----------|-----------|----------------|-------------|
| **Naive (full context)** | $50,000 | $500 | **$50,500** |
| **MemoryKit** | $500 | $82 | **$582** |
| **Savings** | | | **$49,918 (98.8%)** |

---

# **3. SOLUTION OVERVIEW**

## **3.1 Core Concept**

MemoryKit implements a **three-layer memory hierarchy** inspired by human cognition:

```
┌──────────────────────────────────────────────────────┐
│                                                      │
│  Layer 3: Working Memory (L3)                       │
│  ┌────────────────────────────────────────────┐     │
│  │  Redis Cache (5-10 recent items)           │     │
│  │  Sub-5ms retrieval                         │     │
│  │  Hot context for active conversations      │     │
│  └────────────────────────────────────────────┘     │
│                                                      │
│  Layer 2: Semantic Memory (L2)                      │
│  ┌────────────────────────────────────────────┐     │
│  │  Azure Table Storage                       │     │
│  │  Extracted facts & entities                │     │
│  │  Vector-indexed, ~30ms retrieval           │     │
│  └────────────────────────────────────────────┘     │
│                                                      │
│  Layer 1: Episodic Memory (L1)                      │
│  ┌────────────────────────────────────────────┐     │
│  │  Azure Blob + AI Search                    │     │
│  │  Full conversation archive                 │     │
│  │  Vector search, ~120ms retrieval           │     │
│  └────────────────────────────────────────────┘     │
│                                                      │
│  Layer P: Procedural Memory (P) ⭐ NOVEL             │
│  ┌────────────────────────────────────────────┐     │
│  │  Learned workflows & routines              │     │
│  │  "Always format code in Python"            │     │
│  │  "User prefers bullet points"              │     │
│  └────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────┘
```

## **3.2 Intelligent Orchestration**

The **Prefrontal Controller** (inspired by human prefrontal cortex) determines which layers to query:

```csharp
public enum QueryType
{
    Continuation,      // "continue" → L3 only
    FactRetrieval,     // "what was X?" → L2 + L3
    DeepRecall,        // "quote me exactly" → L1 + L2 + L3
    Complex,           // "compare X to Y" → All layers
    ProceduralTrigger  // "write code" → L3 + P
}
```

## **3.3 System Flow**

```
User Query
    ↓
┌──────────────────────┐
│ QueryClassifier      │ ← Determines query type
└──────────────────────┘
    ↓
┌──────────────────────┐
│ PrefrontalController │ ← Creates QueryPlan
└──────────────────────┘
    ↓
┌──────────────────────┐
│ MemoryOrchestrator   │ ← Retrieves from layers
└──────────────────────┘
    ↓
┌──────────────────────┐
│ ContextAssembler     │ ← Builds minimal context
└──────────────────────┘
    ↓
┌──────────────────────┐
│ LLM (Semantic Kernel)│ ← Generates response
└──────────────────────┘
    ↓
Response + Metadata
```

---

# **4. SYSTEM ARCHITECTURE**

## **4.1 Clean Architecture Layers**

```
┌─────────────────────────────────────────────────────────┐
│                  Presentation Layer                      │
│  ┌────────────────────────────────────────────────┐     │
│  │  MemoryKit.API (ASP.NET Core Web API)          │     │
│  │  - REST endpoints                              │     │
│  │  - OpenAPI/Swagger                             │     │
│  │  - Authentication/Authorization                │     │
│  └────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                       │
│  ┌────────────────────────────────────────────────┐     │
│  │  MemoryKit.Application                         │     │
│  │  - Use Cases (CQRS + MediatR)                  │     │
│  │  - DTOs & Mapping                              │     │
│  │  - Validation (FluentValidation)              │     │
│  │  - Orchestration Services                      │     │
│  └────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                    Domain Layer                          │
│  ┌────────────────────────────────────────────────┐     │
│  │  MemoryKit.Domain                              │     │
│  │  - Entities (Message, Fact, Pattern)           │     │
│  │  - Value Objects                               │     │
│  │  - Domain Services                             │     │
│  │  - Interfaces (no implementations)             │     │
│  └────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                Infrastructure Layer                      │
│  ┌────────────────────────────────────────────────┐     │
│  │  MemoryKit.Infrastructure                      │     │
│  │  - Azure Service Implementations               │     │
│  │  - Semantic Kernel Integration                 │     │
│  │  - Repository Implementations                  │     │
│  │  - External Service Adapters                   │     │
│  └────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
```

## **4.2 Project Structure**

```
MemoryKit/
├── src/
│   ├── MemoryKit.Domain/
│   │   ├── Entities/
│   │   │   ├── Message.cs
│   │   │   ├── Conversation.cs
│   │   │   ├── ExtractedFact.cs
│   │   │   └── ProceduralPattern.cs
│   │   ├── ValueObjects/
│   │   │   ├── ImportanceScore.cs
│   │   │   ├── EmbeddingVector.cs
│   │   │   └── QueryPlan.cs
│   │   ├── Enums/
│   │   │   ├── QueryType.cs
│   │   │   ├── EntityType.cs
│   │   │   └── MemoryLayer.cs
│   │   ├── Interfaces/
│   │   │   ├── IMemoryLayer.cs
│   │   │   ├── IMemoryOrchestrator.cs
│   │   │   └── IImportanceEngine.cs
│   │   └── Services/
│   │       └── DomainServices.cs
│   │
│   ├── MemoryKit.Application/
│   │   ├── UseCases/
│   │   │   ├── AddMessage/
│   │   │   │   ├── AddMessageCommand.cs
│   │   │   │   └── AddMessageHandler.cs
│   │   │   ├── QueryMemory/
│   │   │   │   ├── QueryMemoryQuery.cs
│   │   │   │   └── QueryMemoryHandler.cs
│   │   │   └── GetContext/
│   │   ├── DTOs/
│   │   ├── Mapping/
│   │   ├── Validators/
│   │   └── Services/
│   │       ├── ConversationManager.cs
│   │       ├── MemoryOrchestrator.cs
│   │       └── PrefrontalController.cs
│   │
│   ├── MemoryKit.Infrastructure/
│   │   ├── Azure/
│   │   │   ├── WorkingMemoryService.cs (Redis)
│   │   │   ├── ScratchpadService.cs (Table Storage)
│   │   │   ├── EpisodicMemoryService.cs (Blob + AI Search)
│   │   │   └── ProceduralMemoryService.cs (Table Storage)
│   │   ├── SemanticKernel/
│   │   │   ├── SemanticKernelService.cs
│   │   │   ├── QueryClassifier.cs
│   │   │   └── EntityExtractor.cs
│   │   ├── Cognitive/
│   │   │   ├── AmygdalaImportanceEngine.cs
│   │   │   └── HippocampusIndexer.cs
│   │   └── InMemory/ (for testing/MVP)
│   │       ├── InMemoryWorkingMemory.cs
│   │       └── InMemoryStorage.cs
│   │
│   └── MemoryKit.API/
│       ├── Controllers/
│       │   ├── ConversationsController.cs
│       │   ├── MemoriesController.cs
│       │   └── PatternsController.cs
│       ├── Middleware/
│       ├── Filters/
│       └── Program.cs
│
├── tests/
│   ├── MemoryKit.Domain.Tests/
│   ├── MemoryKit.Application.Tests/
│   ├── MemoryKit.Infrastructure.Tests/
│   ├── MemoryKit.API.Tests/
│   └── MemoryKit.IntegrationTests/
│
├── samples/
│   ├── MemoryKit.ConsoleDemo/
│   └── MemoryKit.BlazorDemo/
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── API.md
│   ├── DEPLOYMENT.md
│   └── COGNITIVE_MODEL.md
│
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── release.yml
│
├── README.md
├── CONTRIBUTING.md
├── LICENSE
└── MemoryKit.sln
```

---

# **5. COGNITIVE MODEL MAPPING**

## **5.1 Neuroscience → Software Architecture**

| Brain Component | Function | Software Implementation | Technology |
|----------------|----------|------------------------|------------|
| **Prefrontal Cortex** | Executive function, attention control | `PrefrontalController` | C# orchestration logic |
| **Hippocampus** | Short-term indexing, consolidation | `HippocampusIndexer` | Azure Table Storage |
| **Neocortex** | Long-term semantic storage | `ScratchpadService` | Table Storage + embeddings |
| **Amygdala** | Emotional tagging, importance | `AmygdalaImportanceEngine` | Sentiment analysis + rules |
| **Basal Ganglia** | Procedural memory, habits | `ProceduralMemoryService` | Pattern matching engine |
| **Working Memory** | Active processing (7±2 items) | `WorkingMemoryService` | Redis with LRU eviction |

## **5.2 Memory Consolidation Process**

Inspired by human sleep-based consolidation:

```
┌─────────────────────────────────────────────────────┐
│  1. ENCODING (User interaction)                     │
│     ↓                                                │
│  2. HIPPOCAMPAL INDEXING (Temporary storage)        │
│     ↓                                                │
│  3. IMPORTANCE SCORING (Amygdala weighting)         │
│     ↓                                                │
│  4. CONSOLIDATION (Move to long-term)               │
│     - Extract entities → Scratchpad                 │
│     - Detect patterns → Procedural Memory           │
│     - Archive full text → Episodic Memory           │
│     ↓                                                │
│  5. RETRIEVAL (Query-dependent layer selection)     │
└─────────────────────────────────────────────────────┘
```

## **5.3 Importance Scoring Algorithm**

```csharp
public class ImportanceScore : ValueObject
{
    public double BaseScore { get; init; }      // 0.0 - 1.0
    public double EmotionalWeight { get; init; } // Amygdala contribution
    public double NoveltyBoost { get; init; }    // New information bonus
    public double RecencyFactor { get; init; }   // Time decay
    
    public double FinalScore => 
        (BaseScore * 0.4) +
        (EmotionalWeight * 0.3) +
        (NoveltyBoost * 0.2) +
        (RecencyFactor * 0.1);
}
```

**Importance Triggers:**
- User questions (boost: 1.5x)
- Decisions/commitments ("I will...", "Let's...") (boost: 2.0x)
- Emotional language (sentiment analysis) (boost: 1.3x)
- Novel entities (first mention) (boost: 1.5x)
- Explicit importance cues ("important", "remember") (boost: 2.5x)

---

# **6. COMPONENT SPECIFICATIONS**

## **6.1 MemoryOrchestrator**

**Purpose:** Central coordinator for all memory operations

**Interface:**
```csharp
public interface IMemoryOrchestrator
{
    Task<MemoryContext> RetrieveContextAsync(
        string userId,
        string conversationId,
        string query,
        CancellationToken cancellationToken = default);
    
    Task StoreAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default);
    
    Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default);
}
```

**Implementation:**
```csharp
public class MemoryOrchestrator : IMemoryOrchestrator
{
    private readonly IWorkingMemoryService _workingMemory;
    private readonly IScratchpadService _scratchpad;
    private readonly IEpisodicMemoryService _episodicMemory;
    private readonly IProceduralMemoryService _proceduralMemory;
    private readonly IPrefrontalController _prefrontal;
    private readonly IAmygdalaImportanceEngine _amygdala;
    private readonly ILogger<MemoryOrchestrator> _logger;

    public async Task<MemoryContext> RetrieveContextAsync(
        string userId,
        string conversationId,
        string query,
        CancellationToken cancellationToken)
    {
        // Step 1: Build query plan
        var state = await GetConversationState(conversationId);
        var plan = await _prefrontal.BuildQueryPlanAsync(query, state);
        
        _logger.LogInformation(
            "Query plan: {Type}, Layers: {Layers}",
            plan.Type,
            string.Join(", ", plan.LayersToUse));
        
        // Step 2: Retrieve from layers in parallel
        var tasks = new List<Task>();
        Message[] workingMemoryItems = Array.Empty<Message>();
        ExtractedFact[] facts = Array.Empty<ExtractedFact>();
        Message[] archivedMessages = Array.Empty<Message>();
        ProceduralPattern matchedPattern = null;
        
        if (plan.LayersToUse.Contains(MemoryLayer.WorkingMemory))
        {
            tasks.Add(Task.Run(async () =>
            {
                workingMemoryItems = await _workingMemory.GetRecentAsync(
                    userId,
                    conversationId,
                    count: 10,
                    cancellationToken);
            }, cancellationToken));
        }
        
        if (plan.LayersToUse.Contains(MemoryLayer.SemanticMemory))
        {
            tasks.Add(Task.Run(async () =>
            {
                facts = await _scratchpad.SearchFactsAsync(
                    userId,
                    query,
                    maxResults: 20,
                    cancellationToken);
            }, cancellationToken));
        }
        
        if (plan.LayersToUse.Contains(MemoryLayer.EpisodicMemory))
        {
            tasks.Add(Task.Run(async () =>
            {
                archivedMessages = await _episodicMemory.SearchAsync(
                    userId,
                    query,
                    maxResults: 5,
                    cancellationToken);
            }, cancellationToken));
        }
        
        if (plan.LayersToUse.Contains(MemoryLayer.ProceduralMemory))
        {
            tasks.Add(Task.Run(async () =>
            {
                matchedPattern = await _proceduralMemory.MatchPatternAsync(
                    userId,
                    query,
                    cancellationToken);
            }, cancellationToken));
        }
        
        await Task.WhenAll(tasks);
        
        // Step 3: Assemble context
        return new MemoryContext
        {
            WorkingMemory = workingMemoryItems,
            Facts = facts,
            ArchivedMessages = archivedMessages,
            AppliedProcedure = matchedPattern,
            QueryPlan = plan,
            TotalTokens = CalculateTokenCount(
                workingMemoryItems,
                facts,
                archivedMessages,
                matchedPattern)
        };
    }
    
    public async Task StoreAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken)
    {
        // Step 1: Calculate importance
        var importance = await _amygdala.CalculateImportanceAsync(
            message,
            cancellationToken);
        
        message.Metadata.ImportanceScore = importance.FinalScore;
        
        // Step 2: Store in all layers (parallel)
        var tasks = new[]
        {
            // Layer 1: Archive everything
            _episodicMemory.ArchiveAsync(message, cancellationToken),
            
            // Layer 3: Update working memory
            _workingMemory.AddAsync(
                userId,
                conversationId,
                message,
                cancellationToken)
        };
        
        await Task.WhenAll(tasks);
        
        // Step 3: Background processing (fire-and-forget with error handling)
        _ = Task.Run(async () =>
        {
            try
            {
                // Extract entities for scratchpad
                var entities = await ExtractEntitiesAsync(message);
                if (entities.Any())
                {
                    await _scratchpad.StoreFactsAsync(
                        userId,
                        conversationId,
                        entities,
                        cancellationToken);
                }
                
                // Detect procedural patterns
                await _proceduralMemory.DetectAndStorePatternAsync(
                    userId,
                    message,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background processing failed for message {MessageId}", message.Id);
            }
        }, cancellationToken);
    }
}
```

## **6.2 PrefrontalController**

**Purpose:** Query classification and retrieval strategy

```csharp
public class PrefrontalController : IPrefrontalController
{
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<PrefrontalController> _logger;
    
    public async Task<QueryPlan> BuildQueryPlanAsync(
        string query,
        ConversationState state,
        CancellationToken cancellationToken = default)
    {
        // Fast rule-based classification first
        var quickType = QuickClassify(query);
        if (quickType.HasValue)
        {
            return CreatePlan(quickType.Value, state);
        }
        
        // Use LLM for complex queries
        var classifiedType = await _llm.ClassifyQueryAsync(
            query,
            cancellationToken);
        
        return CreatePlan(classifiedType, state);
    }
    
    private QueryType? QuickClassify(string query)
    {
        var lower = query.ToLowerInvariant().Trim();
        
        // Continuation patterns
        if (ContinuationPatterns.Any(p => lower.StartsWith(p)))
            return QueryType.Continuation;
        
        // Fact retrieval patterns
        if (FactRetrievalPatterns.Any(p => lower.Contains(p)))
            return QueryType.FactRetrieval;
        
        // Deep recall patterns
        if (DeepRecallPatterns.Any(p => lower.Contains(p)))
            return QueryType.DeepRecall;
        
        return null; // Needs LLM classification
    }
    
    private QueryPlan CreatePlan(QueryType type, ConversationState state)
    {
        return type switch
        {
            QueryType.Continuation => new QueryPlan
            {
                Type = type,
                LayersToUse = new List<MemoryLayer>
                {
                    MemoryLayer.WorkingMemory
                }
            },
            
            QueryType.FactRetrieval => new QueryPlan
            {
                Type = type,
                LayersToUse = new List<MemoryLayer>
                {
                    MemoryLayer.WorkingMemory,
                    MemoryLayer.SemanticMemory
                }
            },
            
            QueryType.DeepRecall => new QueryPlan
            {
                Type = type,
                LayersToUse = new List<MemoryLayer>
                {
                    MemoryLayer.WorkingMemory,
                    MemoryLayer.SemanticMemory,
                    MemoryLayer.EpisodicMemory
                }
            },
            
            QueryType.Complex => new QueryPlan
            {
                Type = type,
                LayersToUse = Enum.GetValues<MemoryLayer>().ToList()
            },
            
            QueryType.ProceduralTrigger => new QueryPlan
            {
                Type = type,
                LayersToUse = new List<MemoryLayer>
                {
                    MemoryLayer.WorkingMemory,
                    MemoryLayer.ProceduralMemory
                }
            },
            
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
    
    private static readonly string[] ContinuationPatterns = new[]
    {
        "continue", "go on", "and then", "next", "keep going"
    };
    
    private static readonly string[] FactRetrievalPatterns = new[]
    {
        "what was", "what is", "who is", "when did", "where"
    };
    
    private static readonly string[] DeepRecallPatterns = new[]
    {
        "quote", "exactly", "verbatim", "word for word", "precise"
    };
}
```

## **6.3 AmygdalaImportanceEngine**

**Purpose:** Emotional tagging and importance scoring

```csharp
public class AmygdalaImportanceEngine : IAmygdalaImportanceEngine
{
    private readonly ISemanticKernelService _llm;
    private readonly ISentimentAnalyzer _sentiment;
    
    public async Task<ImportanceScore> CalculateImportanceAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        var baseScore = CalculateBaseScore(message);
        var emotionalWeight = await CalculateEmotionalWeightAsync(message);
        var noveltyBoost = CalculateNoveltyBoost(message);
        var recencyFactor = CalculateRecencyFactor(message);
        
        return new ImportanceScore
        {
            BaseScore = baseScore,
            EmotionalWeight = emotionalWeight,
            NoveltyBoost = noveltyBoost,
            RecencyFactor = recencyFactor
        };
    }
    
    private double CalculateBaseScore(Message message)
    {
        double score = 0.5; // Baseline
        
        // Question detection
        if (message.Content.Contains('?'))
            score += 0.2;
        
        // Decision language
        if (DecisionPatterns.Any(p => 
            message.Content.Contains(p, StringComparison.OrdinalIgnoreCase)))
            score += 0.3;
        
        // Explicit importance markers
        if (ImportanceMarkers.Any(m => 
            message.Content.Contains(m, StringComparison.OrdinalIgnoreCase)))
            score += 0.5;
        
        // Code blocks (technical importance)
        if (message.Content.Contains("```"))
            score += 0.15;
        
        return Math.Min(score, 1.0);
    }
    
    private async Task<double> CalculateEmotionalWeightAsync(Message message)
    {
        var sentiment = await _sentiment.AnalyzeAsync(message.Content);
        
        // High absolute sentiment = high importance
        return Math.Abs(sentiment.Score) * 0.5;
    }
    
    private double CalculateNoveltyBoost(Message message)
    {
        // Check if message introduces new entities
        // (Simplified - full implementation uses embedding similarity)
        var newEntityCount = message.Metadata.ExtractedEntities?
            .Count(e => e.IsNovel) ?? 0;
        
        return Math.Min(newEntityCount * 0.1, 0.5);
    }
    
    private double CalculateRecencyFactor(Message message)
    {
        var age = DateTime.UtcNow - message.Timestamp;
        
        // Exponential decay: importance halves every 24 hours
        return Math.Exp(-age.TotalHours / 24.0);
    }
    
    private static readonly string[] DecisionPatterns = new[]
    {
        "i will", "let's", "we should", "i decided", "going to",
        "plan to", "commit to"
    };
    
    private static readonly string[] ImportanceMarkers = new[]
    {
        "important", "critical", "remember", "don't forget",
        "always", "never", "from now on"
    };
}
```

---

# **7. DATA MODELS**

## **7.1 Domain Entities**

### **Message**

```csharp
public class Message : Entity<string>
{
    public string UserId { get; private set; }
    public string ConversationId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; }
    public DateTime Timestamp { get; private set; }
    public MessageMetadata Metadata { get; private set; }
    
    // Factory method
    public static Message Create(
        string userId,
        string conversationId,
        MessageRole role,
        string content)
    {
        return new Message
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            ConversationId = conversationId,
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            Metadata = MessageMetadata.Default()
        };
    }
    
    // Domain behavior
    public void MarkAsImportant(double score)
    {
        Metadata = Metadata with { ImportanceScore = score };
    }
}

public record MessageMetadata
{
    public bool IsUserQuestion { get; init; }
    public bool ContainsDecision { get; init; }
    public bool ContainsCode { get; init; }
    public string[] Tags { get; init; } = Array.Empty<string>();
    public double ImportanceScore { get; init; }
    public ExtractedEntity[] ExtractedEntities { get; init; } = Array.Empty<ExtractedEntity>();
    
    public static MessageMetadata Default() => new();
}

public enum MessageRole
{
    User,
    Assistant,
    System
}
```

### **ExtractedFact**

```csharp
public class ExtractedFact : Entity<string>
{
    public string UserId { get; private set; }
    public string ConversationId { get; private set; }
    public string Key { get; private set; }
    public string Value { get; private set; }
    public EntityType Type { get; private set; }
    public double Importance { get; private set; }
    public DateTime LastAccessed { get; private set; }
    public int AccessCount { get; private set; }
    public float[] Embedding { get; private set; }
    
    public void RecordAccess()
    {
        LastAccessed = DateTime.UtcNow;
        AccessCount++;
    }
    
    public bool ShouldEvict(TimeSpan ttl, int minAccessCount)
    {
        return AccessCount < minAccessCount &&
               (DateTime.UtcNow - LastAccessed) > ttl;
    }
}

public enum EntityType
{
    Person,
    Place,
    Technology,
    Decision,
    Preference,
    Constraint,
    Goal,
    Other
}
```

### **ProceduralPattern**

```csharp
public class ProceduralPattern : Entity<string>
{
    public string UserId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public PatternTrigger[] Triggers { get; private set; }
    public string InstructionTemplate { get; private set; }
    public double ConfidenceThreshold { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime LastUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public bool Matches(string query, double similarity)
    {
        return similarity >= ConfidenceThreshold;
    }
    
    public void RecordUsage()
    {
        UsageCount++;
        LastUsed = DateTime.UtcNow;
        
        // Reinforcement learning: increase confidence with usage
        if (UsageCount > 10 && ConfidenceThreshold > 0.7)
        {
            ConfidenceThreshold = Math.Max(0.6, ConfidenceThreshold - 0.05);
        }
    }
}

public record PatternTrigger
{
    public TriggerType Type { get; init; }
    public string Pattern { get; init; }
    public float[] Embedding { get; init; }
}

public enum TriggerType
{
    Keyword,
    Regex,
    Semantic
}
```

## **7.2 Value Objects**

### **MemoryContext**

```csharp
public record MemoryContext
{
    public Message[] WorkingMemory { get; init; } = Array.Empty<Message>();
    public ExtractedFact[] Facts { get; init; } = Array.Empty<ExtractedFact>();
    public Message[] ArchivedMessages { get; init; } = Array.Empty<Message>();
    public ProceduralPattern? AppliedProcedure { get; init; }
    public QueryPlan QueryPlan { get; init; }
    public int TotalTokens { get; init; }
    
    public string ToPromptContext()
    {
        var sb = new StringBuilder();
        
        // Apply procedural instruction if matched
        if (AppliedProcedure != null)
        {
            sb.AppendLine($"[SYSTEM INSTRUCTION]: {AppliedProcedure.InstructionTemplate}");
            sb.AppendLine();
        }
        
        // Recent conversation
        if (WorkingMemory.Any())
        {
            sb.AppendLine("=== Recent Conversation ===");
            foreach (var msg in WorkingMemory.OrderBy(m => m.Timestamp))
            {
                sb.AppendLine($"{msg.Role}: {msg.Content}");
            }
            sb.AppendLine();
        }
        
        // Relevant facts
        if (Facts.Any())
        {
            sb.AppendLine("=== Relevant Facts ===");
            foreach (var fact in Facts.OrderByDescending(f => f.Importance).Take(10))
            {
                sb.AppendLine($"- {fact.Key}: {fact.Value}");
            }
            sb.AppendLine();
        }
        
        // Archived context (if deep recall needed)
        if (ArchivedMessages.Any())
        {
            sb.AppendLine("=== Previous Relevant Exchanges ===");
            foreach (var msg in ArchivedMessages.OrderBy(m => m.Timestamp))
            {
                sb.AppendLine($"[{msg.Timestamp:yyyy-MM-dd HH:mm}] {msg.Role}: {msg.Content}");
            }
        }
        
        return sb.ToString();
    }
}
```

### **QueryPlan**

```csharp
public record QueryPlan
{
    public QueryType Type { get; init; }
    public List<MemoryLayer> LayersToUse { get; init; } = new();
    public ProceduralPattern? SuggestedProcedure { get; init; }
    public int EstimatedTokens { get; init; }
}

public enum QueryType
{
    Continuation,
    FactRetrieval,
    DeepRecall,
    Complex,
    ProceduralTrigger
}

public enum MemoryLayer
{
    WorkingMemory,
    SemanticMemory,
    EpisodicMemory,
    ProceduralMemory
}
```

---

# **8. API SPECIFICATION**

## **8.1 REST Endpoints**

### **Base URL:** `https://api.memorykit.dev/v1`

### **Authentication:** Bearer token (JWT) or API Key

---

### **Conversations**

#### **POST /conversations**
Create a new conversation

**Request:**
```json
{
  "userId": "user_123",
  "metadata": {
    "title": "Technical Discussion",
    "tags": ["coding", "architecture"]
  }
}
```

**Response:**
```json
{
  "conversationId": "conv_abc123",
  "userId": "user_123",
  "createdAt": "2025-11-16T10:30:00Z",
  "metadata": { ... }
}
```

---

#### **POST /conversations/{conversationId}/messages**
Add a message to conversation

**Request:**
```json
{
  "role": "user",
  "content": "How do I implement caching in .NET?"
}
```

**Response:**
```json
{
  "messageId": "msg_xyz789",
  "conversationId": "conv_abc123",
  "role": "user",
  "content": "How do I implement caching in .NET?",
  "timestamp": "2025-11-16T10:31:00Z",
  "metadata": {
    "importanceScore": 0.75,
    "extractedEntities": [
      {
        "key": "Technology",
        "value": ".NET",
        "type": "Technology"
      }
    ]
  }
}
```

---

#### **POST /conversations/{conversationId}/query**
Query with automatic memory retrieval

**Request:**
```json
{
  "question": "What caching strategies did we discuss?",
  "options": {
    "maxTokens": 2000,
    "includeDebugInfo": true
  }
}
```

**Response:**
```json
{
  "answer": "We discussed three caching strategies: 1) In-memory caching with IMemoryCache, 2) Distributed caching with Redis, and 3) Response caching for HTTP responses.",
  "sources": [
    {
      "type": "SemanticMemory",
      "content": "Technology: .NET, Caching",
      "relevanceScore": 0.92
    },
    {
      "type": "WorkingMemory",
      "messageId": "msg_xyz789",
      "timestamp": "2025-11-16T10:31:00Z"
    }
  ],
  "debugInfo": {
    "queryPlan": {
      "type": "FactRetrieval",
      "layersUsed": ["WorkingMemory", "SemanticMemory"]
    },
    "tokensUsed": 450,
    "retrievalTimeMs": 85
  }
}
```

---

### **Memory Operations**

#### **GET /conversations/{conversationId}/context**
Get assembled memory context

**Query Parameters:**
- `query` (optional): Filter context by relevance
- `layers` (optional): Comma-separated layers to include

**Response:**
```json
{
  "workingMemory": [ ... ],
  "facts": [ ... ],
  "archivedMessages": [ ... ],
  "appliedProcedure": { ... },
  "totalTokens": 1250
}
```

---

#### **GET /users/{userId}/patterns**
List procedural patterns

**Response:**
```json
{
  "patterns": [
    {
      "id": "pattern_123",
      "name": "Code Formatting Preference",
      "description": "User prefers Python code with type hints",
      "triggers": [
        {
          "type": "Keyword",
          "pattern": "write code"
        }
      ],
      "usageCount": 25,
      "lastUsed": "2025-11-15T14:20:00Z"
    }
  ]
}
```

---

#### **DELETE /users/{userId}**
GDPR compliance: Delete all user data

**Response:**
```json
{
  "deletedAt": "2025-11-16T10:45:00Z",
  "itemsDeleted": {
    "conversations": 15,
    "messages": 342,
    "facts": 128,
    "patterns": 8
  }
}
```

---

## **8.2 SDK Example (.NET)**

```csharp
using MemoryKit.Client;

var client = new MemoryKitClient("your-api-key");

// Create conversation
var conversation = await client.Conversations.CreateAsync("user_123");

// Add messages
await client.Conversations.AddMessageAsync(
    conversation.Id,
    MessageRole.User,
    "I'm building a .NET API with caching");

// Query with automatic context
var response = await client.Conversations.QueryAsync(
    conversation.Id,
    "What caching approach should I use?");

Console.WriteLine(response.Answer);
Console.WriteLine($"Tokens used: {response.DebugInfo.TokensUsed}");
```

---

# **9. MEMORY LAYER IMPLEMENTATIONS**

## **9.1 Layer 3: Working Memory (Redis)**

```csharp
public class WorkingMemoryService : IWorkingMemoryService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<WorkingMemoryService> _logger;
    private const int MAX_ITEMS = 10;
    
    public async Task AddAsync(
        string userId,
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(userId, conversationId);
        var db = _redis.GetDatabase();
        
        // Calculate score: (timestamp * 1000) + (importance * 100)
        var score = (message.Timestamp.Ticks / 10000) +
                   (message.Metadata.ImportanceScore * 100);
        
        var json = JsonSerializer.Serialize(message);
        
        await db.SortedSetAddAsync(key, json, score);
        
        // Keep only top MAX_ITEMS
        await db.SortedSetRemoveRangeByRankAsync(
            key,
            0,
            -MAX_ITEMS - 1);
        
        // Set TTL: 1 hour idle expiration
        await db.KeyExpireAsync(key, TimeSpan.FromHours(1));
        
        _logger.LogDebug(
            "Added message {MessageId} to working memory (score: {Score})",
            message.Id,
            score);
    }
    
    public async Task<Message[]> GetRecentAsync(
        string userId,
        string conversationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(userId, conversationId);
        var db = _redis.GetDatabase();
        
        var items = await db.SortedSetRangeByScoreAsync(
            key,
            order: Order.Descending,
            take: count);
        
        return items
            .Select(item => JsonSerializer.Deserialize<Message>(item.ToString()))
            .ToArray();
    }
    
    private static string GetKey(string userId, string conversationId)
        => $"wm:{userId}:{conversationId}";
}
```

---

## **9.2 Layer 2: Scratchpad (Azure Table Storage)**

```csharp
public class ScratchpadService : IScratchpadService
{
    private readonly TableClient _tableClient;
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<ScratchpadService> _logger;
    
    public async Task StoreFactsAsync(
        string userId,
        string conversationId,
        ExtractedEntity[] entities,
        CancellationToken cancellationToken = default)
    {
        var batch = new List<TableTransactionAction>();
        
        foreach (var entity in entities)
        {
            var factEntity = new FactTableEntity
            {
                PartitionKey = userId,
                RowKey = $"{conversationId}_{entity.Key}_{Guid.NewGuid():N}",
                Key = entity.Key,
                Value = entity.Value,
                Type = entity.Type.ToString(),
                Importance = entity.Importance,
                LastAccessed = DateTime.UtcNow,
                AccessCount = 1,
                ConversationId = conversationId,
                Embedding = JsonSerializer.Serialize(entity.Embedding)
            };
            
            batch.Add(new TableTransactionAction(
                TableTransactionActionType.UpsertReplace,
                factEntity));
            
            // Table Storage batch limit: 100 operations
            if (batch.Count == 100)
            {
                await _tableClient.SubmitTransactionAsync(batch, cancellationToken);
                batch.Clear();
            }
        }
        
        if (batch.Any())
        {
            await _tableClient.SubmitTransactionAsync(batch, cancellationToken);
        }
        
        _logger.LogInformation(
            "Stored {Count} facts for user {UserId}",
            entities.Length,
            userId);
    }
    
    public async Task<ExtractedFact[]> SearchFactsAsync(
        string userId,
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        // Get query embedding
        var queryEmbedding = await _llm.GetEmbeddingAsync(query, cancellationToken);
        
        // Retrieve all facts for user (partitioned query)
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {userId}");
        var facts = new List<FactTableEntity>();
        
        await foreach (var entity in _tableClient.QueryAsync<FactTableEntity>(
            filter,
            cancellationToken: cancellationToken))
        {
            facts.Add(entity);
        }
        
        // Rank by cosine similarity
        var ranked = facts
            .Select(f => new
            {
                Fact = f,
                Similarity = CosineSimilarity(
                    queryEmbedding,
                    JsonSerializer.Deserialize<float[]>(f.Embedding))
            })
            .OrderByDescending(x => x.Similarity)
            .Take(maxResults)
            .ToArray();
        
        // Update access tracking
        var updateBatch = ranked
            .Select(r =>
            {
                r.Fact.LastAccessed = DateTime.UtcNow;
                r.Fact.AccessCount++;
                return new TableTransactionAction(
                    TableTransactionActionType.UpdateReplace,
                    r.Fact);
            })
            .ToList();
        
        if (updateBatch.Any())
        {
            // Process in batches of 100
            foreach (var chunk in updateBatch.Chunk(100))
            {
                await _tableClient.SubmitTransactionAsync(chunk.ToList(), cancellationToken);
            }
        }
        
        return ranked
            .Select(r => ToExtractedFact(r.Fact))
            .ToArray();
    }
    
    private static double CosineSimilarity(float[] a, float[] b)
    {
        var dotProduct = a.Zip(b, (x, y) => x * y).Sum();
        var magnitudeA = Math.Sqrt(a.Sum(x => x * x));
        var magnitudeB = Math.Sqrt(b.Sum(x => x * x));
        return dotProduct / (magnitudeA * magnitudeB);
    }
}

public class FactTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } // UserId
    public string RowKey { get; set; } // ConversationId_Key_Guid
    public string Key { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }
    public double Importance { get; set; }
    public DateTime LastAccessed { get; set; }
    public int AccessCount { get; set; }
    public string ConversationId { get; set; }
    public string Embedding { get; set; } // JSON serialized
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

---

## **9.3 Layer 1: Episodic Memory (Blob + AI Search)**

```csharp
public class EpisodicMemoryService : IEpisodicMemoryService
{
    private readonly BlobContainerClient _blobClient;
    private readonly SearchClient _searchClient;
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<EpisodicMemoryService> _logger;
    
    public async Task ArchiveAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Store in Blob
        var blobName = GetBlobPath(message);
        var json = JsonSerializer.Serialize(message);
        
        await _blobClient.UploadBlobAsync(
            blobName,
            BinaryData.FromString(json),
            cancellationToken);
        
        // Step 2: Generate embedding
        var embedding = await _llm.GetEmbeddingAsync(
            message.Content,
            cancellationToken);
        
        // Step 3: Index in AI Search
        var document = new SearchDocument
        {
            ["id"] = message.Id,
            ["userId"] = message.UserId,
            ["conversationId"] = message.ConversationId,
            ["role"] = message.Role.ToString(),
            ["content"] = message.Content,
            ["contentVector"] = embedding,
            ["timestamp"] = message.Timestamp,
            ["importanceScore"] = message.Metadata.ImportanceScore,
            ["blobPath"] = blobName
        };
        
        var batch = IndexDocumentsBatch.Upload(new[] { document });
        await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
        
        _logger.LogDebug(
            "Archived message {MessageId} to blob and search index",
            message.Id);
    }
    
    public async Task<Message[]> SearchAsync(
        string userId,
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        // Generate query embedding
        var queryEmbedding = await _llm.GetEmbeddingAsync(query, cancellationToken);
        
        // Vector search with user filter
        var searchOptions = new SearchOptions
        {
            Filter = $"userId eq '{userId}'",
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(queryEmbedding)
                    {
                        KNearestNeighborsCount = maxResults,
                        Fields = { "contentVector" }
                    }
                }
            },
            Size = maxResults,
            Select = { "blobPath", "importanceScore" }
        };
        
        var searchResults = await _searchClient.SearchAsync<SearchDocument>(
            null,
            searchOptions,
            cancellationToken);
        
        // Retrieve full messages from Blob
        var messages = new List<Message>();
        
        await foreach (var result in searchResults.Value.GetResultsAsync())
        {
            var blobPath = result.Document["blobPath"].ToString();
            var blob = _blobClient.GetBlobClient(blobPath);
            
            var response = await blob.DownloadContentAsync(cancellationToken);
            var message = JsonSerializer.Deserialize<Message>(
                response.Value.Content.ToString());
            
            messages.Add(message);
        }
        
        return messages.ToArray();
    }
    
    private static string GetBlobPath(Message message)
        => $"{message.UserId}/{message.ConversationId}/{message.Id}.json";
}
```

---

## **9.4 Layer P: Procedural Memory**

```csharp
public class ProceduralMemoryService : IProceduralMemoryService
{
    private readonly TableClient _tableClient;
    private readonly ISemanticKernelService _llm;
    private readonly ILogger<ProceduralMemoryService> _logger;
    
    public async Task<ProceduralPattern?> MatchPatternAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        // Retrieve user's patterns
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {userId}");
        var patterns = new List<PatternTableEntity>();
        
        await foreach (var entity in _tableClient.QueryAsync<PatternTableEntity>(
            filter,
            cancellationToken: cancellationToken))
        {
            patterns.Add(entity);
        }
        
        if (!patterns.Any())
            return null;
        
        // Get query embedding
        var queryEmbedding = await _llm.GetEmbeddingAsync(query, cancellationToken);
        
        // Find best match
        ProceduralPattern? bestMatch = null;
        double bestSimilarity = 0;
        
        foreach (var pattern in patterns)
        {
            var patternObj = ToProceduralPattern(pattern);
            
            // Check semantic triggers
            foreach (var trigger in patternObj.Triggers.Where(t => t.Type == TriggerType.Semantic))
            {
                var similarity = CosineSimilarity(queryEmbedding, trigger.Embedding);
                
                if (similarity > bestSimilarity && patternObj.Matches(query, similarity))
                {
                    bestSimilarity = similarity;
                    bestMatch = patternObj;
                }
            }
            
            // Check keyword triggers
            foreach (var trigger in patternObj.Triggers.Where(t => t.Type == TriggerType.Keyword))
            {
                if (query.Contains(trigger.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    bestMatch = patternObj;
                    break;
                }
            }
        }
        
        // Record usage if matched
        if (bestMatch != null)
        {
            bestMatch.RecordUsage();
            await UpdatePatternUsageAsync(bestMatch, cancellationToken);
        }
        
        return bestMatch;
    }
    
    public async Task DetectAndStorePatternAsync(
        string userId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        // Use LLM to detect if message contains procedural instruction
        var detectionPrompt = $@"
Does this message contain a procedural instruction or preference that should be remembered?
Examples: 'always format code in Python', 'use bullet points for lists', 'avoid technical jargon'

Message: {message.Content}

Respond with JSON:
{{
  ""isProceduralInstruction"": true/false,
  ""name"": ""brief name"",
  ""description"": ""what to remember"",
  ""instructionTemplate"": ""how to apply this""
}}";
        
        var response = await _llm.CompleteAsync(detectionPrompt, cancellationToken);
        
        // Parse and store if valid
        try
        {
            var detection = JsonSerializer.Deserialize<PatternDetection>(response);
            
            if (detection.IsProceduralInstruction)
            {
                var pattern = new ProceduralPattern
                {
                    UserId = userId,
                    Name = detection.Name,
                    Description = detection.Description,
                    InstructionTemplate = detection.InstructionTemplate,
                    ConfidenceThreshold = 0.75, // Initial threshold
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    Triggers = await GenerateTriggersAsync(message.Content, cancellationToken)
                };
                
                await StorePatternAsync(pattern, cancellationToken);
                
                _logger.LogInformation(
                    "Detected and stored new procedural pattern: {PatternName}",
                    pattern.Name);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse pattern detection response");
        }
    }
    
    private async Task<PatternTrigger[]> GenerateTriggersAsync(
        string content,
        CancellationToken cancellationToken)
    {
        var triggers = new List<PatternTrigger>();
        
        // Extract keywords (simplified - use NLP in production)
        var keywords = content
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .Take(3);
        
        foreach (var keyword in keywords)
        {
            triggers.Add(new PatternTrigger
            {
                Type = TriggerType.Keyword,
                Pattern = keyword.ToLowerInvariant()
            });
        }
        
        // Add semantic trigger
        var embedding = await _llm.GetEmbeddingAsync(content, cancellationToken);
        triggers.Add(new PatternTrigger
        {
            Type = TriggerType.Semantic,
            Pattern = content,
            Embedding = embedding
        });
        
        return triggers.ToArray();
    }
}

public record PatternDetection
{
    public bool IsProceduralInstruction { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string InstructionTemplate { get; init; }
}
```

---

# **10. AI/LLM INTEGRATION**

## **10.1 Semantic Kernel Service**

```csharp
public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService _embeddings;
    private readonly IChatCompletionService _chat;
    private readonly ILogger<SemanticKernelService> _logger;
    
    public SemanticKernelService(
        IConfiguration configuration,
        ILogger<SemanticKernelService> logger)
    {
        _logger = logger;
        
        var builder = Kernel.CreateBuilder();
        
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"],
            configuration["AzureOpenAI:Endpoint"],
            configuration["AzureOpenAI:ApiKey"]);
        
        builder.AddAzureOpenAITextEmbeddingGeneration(
            configuration["AzureOpenAI:EmbeddingDeployment"],
            configuration["AzureOpenAI:Endpoint"],
            configuration["AzureOpenAI:ApiKey"]);
        
        _kernel = builder.Build();
        _embeddings = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _chat = _kernel.GetRequiredService<IChatCompletionService>();
    }
    
    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddings.GenerateEmbeddingAsync(
            text,
            cancellationToken: cancellationToken);
        
        return embedding.ToArray();
    }
    
    public async Task<QueryType> ClassifyQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"
Classify this query into one of these types:
- Continuation: User wants to continue previous topic
- FactRetrieval: User asking for specific information
- DeepRecall: User wants exact quotes or detailed history
- Complex: Multi-faceted question requiring deep analysis
- ProceduralTrigger: Task that matches a learned routine

Query: {query}

Respond with ONLY the classification type.";
        
        var result = await _chat.GetChatMessageContentAsync(
            prompt,
            cancellationToken: cancellationToken);
        
        return Enum.Parse<QueryType>(result.Content.Trim(), ignoreCase: true);
    }
    
    public async Task<ExtractedEntity[]> ExtractEntitiesAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"
Extract key entities from this text. Return JSON array.

Text: {text}

Format:
[
  {{
    ""key"": ""ProjectName"",
    ""value"": ""MemoryKit"",
    ""type"": ""Technology"",
    ""importance"": 0.9
  }}
]

Types: Person, Place, Technology, Decision, Preference, Constraint, Goal, Other";
        
        var result = await _chat.GetChatMessageContentAsync(
            prompt,
            cancellationToken: cancellationToken);
        
        var entities = JsonSerializer.Deserialize<ExtractedEntity[]>(
            result.Content.Trim());
        
        // Generate embeddings for each entity
        foreach (var entity in entities)
        {
            entity.Embedding = await GetEmbeddingAsync(
                $"{entity.Key}: {entity.Value}",
                cancellationToken);
        }
        
        return entities;
    }
    
    public async Task<string> AnswerWithContextAsync(
        string query,
        MemoryContext context,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = context.ToPromptContext();
        
        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(query);
        
        var result = await _chat.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);
        
        return result.Content;
    }
}
```

---

# **11. PERFORMANCE REQUIREMENTS**

## **11.1 Latency Targets**

| Operation | Target | P99 | Notes |
|-----------|--------|-----|-------|
| Working Memory Retrieval | < 5ms | < 10ms | Redis in-memory |
| Scratchpad Search | < 30ms | < 50ms | Table Storage query + embedding similarity |
| Episodic Vector Search | < 120ms | < 200ms | AI Search with vector index |
| Full Context Assembly | < 150ms | < 250ms | Parallel layer retrieval |
| End-to-End Query | < 2s | < 3s | Including LLM completion |

## **11.2 Throughput**

- **Concurrent conversations:** 10,000+
- **Messages/second:** 1,000+
- **Queries/second:** 500+

## **11.3 Scalability Strategy**

```
┌─────────────────────────────────────────────────┐
│  Azure App Service (Stateless API)              │
│  - Auto-scale: 2-10 instances                   │
│  - Load balancer: Azure Front Door              │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  Redis Cache (Working Memory)                   │
│  - Premium tier: 6GB                            │
│  - Clustering enabled                           │
│  - Max connections: 10,000                      │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  Table Storage (Scratchpad + Procedural)        │
│  - Partitioned by userId                        │
│  - Auto-scaling: built-in                       │
│  - 20,000 ops/sec per partition                 │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  Blob Storage + AI Search (Episodic)            │
│  - Blob: Zone-redundant                         │
│  - AI Search: Standard tier (3 replicas)        │
│  - Index size: scales with data                 │
└─────────────────────────────────────────────────┘
```

## **11.4 Cost Optimization**

### **Per 10,000 Conversations/Month:**

| Component | Cost | Optimization |
|-----------|------|--------------|
| Redis Cache (6GB Premium) | $75/mo | Use basic for dev |
| Table Storage | $2/mo | Minimal |
| Blob Storage | $1/mo | Cool tier for old data |
| AI Search (Standard) | $250/mo | Basic tier for <50GB |
| Azure OpenAI (Embeddings) | $50/mo | Batch processing, caching |
| App Service (P1V2) | $75/mo | Reserved instance discount |
| **Total** | **$453/mo** | **$0.045 per conversation** |

**Cost Savings vs. Naive Approach:**
- Naive: $50 per conversation × 10,000 = **$500,000/mo**
- MemoryKit: **$453/mo**
- **Savings: 99.91%** 🎯

---

# **12. SECURITY & COMPLIANCE**

## **12.1 Authentication & Authorization**

```csharp
// API Key Authentication
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyValidator _validator;
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return AuthenticateResult.Fail("Missing API Key");
        }
        
        var isValid = await _validator.ValidateAsync(apiKey);
        
        if (!isValid)
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }
        
        var claims = new[] { new Claim(ClaimTypes.Name, apiKey) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        
        return AuthenticateResult.Success(ticket);
    }
}
```

## **12.2 Data Encryption**

- **At Rest:** All Azure storage services use AES-256 encryption
- **In Transit:** TLS 1.3 for all API communication
- **Secrets:** Azure Key Vault for API keys, connection strings

## **12.3 Multi-Tenancy Isolation**

```
User A Data:
  - Partition Key: userA_id
  - Blob Path: userA_id/...
  - Redis Key Prefix: wm:userA_id:...
  
User B Data:
  - Partition Key: userB_id
  - Blob Path: userB_id/...
  - Redis Key Prefix: wm:userB_id:...
  
→ Complete logical isolation at storage level
```

## **12.4 GDPR Compliance**

**Right to be Forgotten Implementation:**

```csharp
public class DataDeletionService : IDataDeletionService
{
    public async Task DeleteAllUserDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        // 1. Delete from Working Memory (Redis)
        await DeleteFromRedisAsync(userId, cancellationToken);
        
        // 2. Delete from Scratchpad (Table Storage)
        await DeleteFromTableStorageAsync(userId, cancellationToken);
        
        // 3. Delete from Episodic Memory (Blob + AI Search)
        await DeleteFromBlobStorageAsync(userId, cancellationToken);
        await DeleteFromSearchIndexAsync(userId, cancellationToken);
        
        // 4. Delete from Procedural Memory
        await DeleteProceduralPatternsAsync(userId, cancellationToken);
        
        // 5. Audit log
        await _auditLogger.LogDeletionAsync(userId, DateTime.UtcNow);
    }
}
```

## **12.5 Rate Limiting**

```csharp
// ASP.NET Core 8 rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 20;
    });
});
```

---

# **13. TESTING STRATEGY**

## **13.1 Test Pyramid**

```
        /\
       /  \  E2E Tests (5%)
      /────\
     /      \  Integration Tests (25%)
    /────────\
   /          \  Unit Tests (70%)
  /────────────\
```

## **13.2 Unit Tests**

```csharp
public class MemoryOrchestratorTests
{
    [Fact]
    public async Task RetrieveContext_Continuation_UsesOnlyWorkingMemory()
    {
        // Arrange
        var workingMemory = new Mock<IWorkingMemoryService>();
        var prefrontal = new Mock<IPrefrontalController>();
        
        prefrontal
            .Setup(p => p.BuildQueryPlanAsync(
                It.IsAny<string>(),
                It.IsAny<ConversationState>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryPlan
            {
                Type = QueryType.Continuation,
                LayersToUse = new() { MemoryLayer.WorkingMemory }
            });
        
        var orchestrator = new MemoryOrchestrator(
            workingMemory.Object,
            Mock.Of<IScratchpadService>(),
            Mock.Of<IEpisodicMemoryService>(),
            Mock.Of<IProceduralMemoryService>(),
            prefrontal.Object,
            Mock.Of<IAmygdalaImportanceEngine>(),
            Mock.Of<ILogger<MemoryOrchestrator>>());
        
        // Act
        var context = await orchestrator.RetrieveContextAsync(
            "user123",
            "conv456",
            "continue");
        
        // Assert
        workingMemory.Verify(
            w => w.GetRecentAsync(
                "user123",
                "conv456",
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        Assert.NotNull(context);
        Assert.Equal(QueryType.Continuation, context.QueryPlan.Type);
    }
}
```

## **13.3 Integration Tests**

```csharp
public class EpisodicMemoryIntegrationTests : IClassFixture<AzureTestFixture>
{
    private readonly AzureTestFixture _fixture;
    
    public EpisodicMemoryIntegrationTests(AzureTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Archive_And_Search_RoundTrip()
    {
        // Arrange
        var service = _fixture.GetEpisodicMemoryService();
        var message = Message.Create(
            "testuser",
            "testconv",
            MessageRole.User,
            "How do I implement CQRS in .NET?");
        
        // Act: Archive
        await service.ArchiveAsync(message);
        
        // Wait for indexing
        await Task.Delay(2000);
        
        // Act: Search
        var results = await service.SearchAsync(
            "testuser",
            "CQRS implementation",
            maxResults: 5);
        
        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Id == message.Id);
    }
}
```

## **13.4 Performance Benchmarks**

```csharp
[MemoryDiagnoser]
public class MemoryRetrievalBenchmarks
{
    private MemoryOrchestrator _orchestrator;
    
    [GlobalSetup]
    public void Setup()
    {
        // Setup with real services
        _orchestrator = CreateOrchestrator();
    }
    
    [Benchmark]
    public async Task WorkingMemoryOnly()
    {
        await _orchestrator.RetrieveContextAsync(
            "user",
            "conv",
            "continue");
    }
    
    [Benchmark]
    public async Task WorkingMemoryPlusScratchpad()
    {
        await _orchestrator.RetrieveContextAsync(
            "user",
            "conv",
            "what was X?");
    }
    
    [Benchmark]
    public async Task FullRetrieval()
    {
        await _orchestrator.RetrieveContextAsync(
            "user",
            "conv",
            "give me detailed history of X");
    }
}
```

**Expected Results:**
```
| Method                        | Mean      | Allocated |
|------------------------------|-----------|-----------|
| WorkingMemoryOnly            | 4.2 ms    | 2.1 KB    |
| WorkingMemoryPlusScratchpad  | 28.5 ms   | 15.3 KB   |
| FullRetrieval                | 125.3 ms  | 45.8 KB   |
```

---

# **14. DEPLOYMENT ARCHITECTURE**

## **14.1 Azure Resources (IaC with Bicep)**

```bicep
// main.bicep
param location string = resourceGroup().location
param environment string = 'dev'

// App Service
resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: 'memorykit-api-${environment}'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: true
    }
  }
}

// Redis Cache
resource redis 'Microsoft.Cache/redis@2022-06-01' = {
  name: 'memorykit-cache-${environment}'
  location: location
  properties: {
    sku: {
      name: 'Premium'
      family: 'P'
      capacity: 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

// Storage Account
resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'memorykitstorage${environment}'
  location: location
  sku: {
    name: 'Standard_ZRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// AI Search
resource search 'Microsoft.Search/searchServices@2022-09-01' = {
  name: 'memorykit-search-${environment}'
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    replicaCount: 3
    partitionCount: 1
  }
}

// Azure OpenAI
resource openai 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'memorykit-openai-${environment}'
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
}
```

## **14.2 CI/CD Pipeline (GitHub Actions)**

```yaml
name: CI/CD

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
      
      - name: Run unit tests
        run: dotnet test --no-build --configuration Release --filter Category=Unit
      
      - name: Run integration tests
        run: dotnet test --no-build --configuration Release --filter Category=Integration
        env:
          AZURE_CONNECTION_STRING: ${{ secrets.AZURE_CONNECTION_STRING }}
      
      - name: Run benchmarks
        run: dotnet run --project tests/MemoryKit.Benchmarks --configuration Release
      
      - name: Publish
        run: dotnet publish src/MemoryKit.API --configuration Release --output ./publish
      
      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: api
          path: ./publish

  deploy-dev:
    needs: build-and-test
    if: github.ref == 'refs/heads/develop'
    runs-on: ubuntu-latest
    environment: development
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: api
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v2
        with:
          app-name: memorykit-api-dev
          package: .

  deploy-prod:
    needs: build-and-test
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment: production
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: api
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v2
        with:
          app-name: memorykit-api-prod
          package: .
```

## **14.3 Environment Configuration**

```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "Azure": {
    "Redis": {
      "ConnectionString": "${REDIS_CONNECTION_STRING}"
    },
    "Storage": {
      "ConnectionString": "${STORAGE_CONNECTION_STRING}",
      "ContainerName": "conversations"
    },
    "TableStorage": {
      "ConnectionString": "${STORAGE_CONNECTION_STRING}",
      "FactsTableName": "facts",
      "PatternsTableName": "patterns"
    },
    "Search": {
      "Endpoint": "${SEARCH_ENDPOINT}",
      "ApiKey": "${SEARCH_API_KEY}",
      "IndexName": "messages"
    }
  },
  "AzureOpenAI": {
    "Endpoint": "${OPENAI_ENDPOINT}",
    "ApiKey": "${OPENAI_API_KEY}",
    "DeploymentName": "gpt-4",
    "EmbeddingDeployment": "text-embedding-ada-002"
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "Window": "00:01:00"
  }
}
```

---

# **15. TECHNOLOGY STACK**

## **15.1 Core Framework**

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 8.0 / 9.0 | Core framework |
| C# | 12 | Language |
| ASP.NET Core | 8.0 | Web API |
| Minimal APIs | 8.0 | Endpoints (alternative) |

## **15.2 Azure Services**

| Service | Purpose | Tier |
|---------|---------|------|
| Azure OpenAI | Embeddings + Completions | Standard |
| Azure AI Search | Vector search | Standard |
| Azure Cache for Redis | Working memory | Premium P1 |
| Azure Blob Storage | Archive | Standard ZRS |
| Azure Table Storage | Facts + patterns | Standard |
| Azure App Service | API hosting | P1V2 |
| Azure Key Vault | Secrets management | Standard |
| Application Insights | Monitoring | Standard |

## **15.3 Libraries & SDKs**

```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.0.0" />
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.12" />
<PackageReference Include="Azure.Search.Documents" Version="11.5.1" />
<PackageReference Include="Azure.Data.Tables" Version="12.8.1" />
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
<PackageReference Include="StackExchange.Redis" Version="2.7.4" />
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="BenchmarkDotNet" Version="0.13.10" />
```

## **15.4 Development Tools**

- **IDE:** Visual Studio 2022 / JetBrains Rider / VS Code
- **Testing:** xUnit, Moq, FluentAssertions
- **Benchmarking:** BenchmarkDotNet
- **API Testing:** Postman, REST Client
- **IaC:** Azure Bicep
- **CI/CD:** GitHub Actions
- **Monitoring:** Application Insights, Azure Monitor

---

# **16. DEVELOPMENT ROADMAP**

## **16.1 Phase 1: Foundation (Weeks 1-2) - MVP**

**Goal:** Working memory system with basic three-layer architecture

### **Week 1: Domain + Infrastructure**
- [ ] Project structure setup
- [ ] Domain models (Message, Conversation, ExtractedFact)
- [ ] Interfaces (IMemoryLayer, IMemoryOrchestrator)
- [ ] In-memory implementations (for testing)
- [ ] Unit tests for domain logic

**Deliverable:** Compilable domain + test project

### **Week 2: Core Memory Layers**
- [ ] WorkingMemoryService (Redis)
- [ ] ScratchpadService (Table Storage)
- [ ] EpisodicMemoryService (Blob + AI Search)
- [ ] MemoryOrchestrator (basic)
- [ ] Integration tests

**Deliverable:** Working memory storage and retrieval

---

## **16.2 Phase 2: Intelligence (Weeks 3-4)**

**Goal:** Add cognitive components and LLM integration

### **Week 3: AI Integration**
- [ ] SemanticKernelService setup
- [ ] Entity extraction
- [ ] Query classification
- [ ] Embedding generation
- [ ] ImportanceEngine (basic scoring)

**Deliverable:** Intelligent entity extraction and query routing

### **Week 4: Procedural Memory**
- [ ] ProceduralMemoryService
- [ ] Pattern detection
- [ ] Pattern matching
- [ ] Automatic learning
- [ ] Integration with orchestrator

**Deliverable:** ⭐ **World's first .NET procedural memory for LLMs**

---

## **16.3 Phase 3: API & Deployment (Week 5)**

**Goal:** Production-ready API with deployment

- [ ] REST API endpoints
- [ ] Authentication (API Key)
- [ ] Rate limiting
- [ ] OpenAPI/Swagger documentation
- [ ] Azure infrastructure (Bicep)
- [ ] CI/CD pipeline
- [ ] Monitoring setup

**Deliverable:** Deployed API on Azure

---

## **16.4 Phase 4: Demo & Documentation (Week 6)**

**Goal:** Showcase project with great documentation

- [ ] Console demo application
- [ ] Blazor UI for visualization
- [ ] Comprehensive README.md
- [ ] ARCHITECTURE.md
- [ ] API documentation
- [ ] Video demo
- [ ] Blog post

**Deliverable:** Complete OSS project ready for GitHub

---

## **16.5 Future Enhancements (Post-MVP)**

### **V2 Features:**
- [ ] Multi-modal memory (images, audio)
- [ ] Memory consolidation scheduler (nightly batch jobs)
- [ ] Advanced pattern learning (reinforcement)
- [ ] Cost analytics dashboard
- [ ] Memory pruning strategies
- [ ] User-defined memory rules

### **V3 Features:**
- [ ] Multi-agent shared memory
- [ ] Federated learning across users
- [ ] Real-time collaborative memory
- [ ] Memory marketplace (shareable patterns)

---

# **17. SUCCESS METRICS**

## **17.1 Technical Metrics**

| Metric | Target | Measurement |
|--------|--------|-------------|
| API Latency (P95) | < 200ms | Application Insights |
| Memory Retrieval Accuracy | > 95% | Manual evaluation + unit tests |
| Token Cost Reduction | > 98% | Comparison with baseline |
| Uptime | > 99.9% | Azure Monitor |
| Code Coverage | > 80% | Coverlet |

## **17.2 Open Source Metrics**

| Metric | 3-Month Target | 6-Month Target |
|--------|----------------|----------------|
| GitHub Stars | 100 | 500 |
| Forks | 20 | 100 |
| Contributors | 3 | 10 |
| NPM/NuGet Downloads | 500 | 2,000 |
| Blog Post Views | 1,000 | 5,000 |

## **17.3 Career Impact Metrics**

**Goals for Antonio's Portfolio:**

✅ **Technical Depth:** Demonstrates advanced .NET, Azure, AI/LLM expertise
✅ **Architecture Skills:** Shows clean architecture, DDD, CQRS mastery
✅ **Innovation:** First .NET implementation of cognitive memory model
✅ **Production Quality:** CI/CD, monitoring, security, compliance
✅ **Open Source Leadership:** Community building, documentation
✅ **Thought Leadership:** Blog posts, conference talks

**Expected Outcomes:**
- Increase interview callback rate to 40%+
- Position for senior/staff engineer roles
- Potential consulting opportunities
- Conference speaking invitations

---

# **18. APPENDICES**

## **18.1 Glossary**

| Term | Definition |
|------|------------|
| **Working Memory** | Short-term, actively used memory (5-10 items) |
| **Semantic Memory** | Factual knowledge, extracted entities |
| **Episodic Memory** | Complete historical archive of interactions |
| **Procedural Memory** | Learned workflows, routines, preferences |
| **Importance Scoring** | Emotional/relevance weighting (0.0-1.0) |
| **Query Plan** | Strategy for which memory layers to use |
| **Memory Context** | Assembled minimal context for LLM |
| **Consolidation** | Moving from short-term to long-term storage |

## **18.2 References**

### **Academic Papers:**
1. "LIGHT: A Memory Architecture for LLMs" (University of Alberta, 2025)
2. "The Neuroscience of Memory" (Kandel et al., 2024)
3. "Attention is All You Need" (Vaswani et al., 2017)
4. "Cognitive Architectures for LLMs" (OpenAI Research, 2024)

### **Industry Solutions:**
1. Mem0 - https://github.com/mem0ai/mem0
2. Letta AI - https://github.com/letta-ai/letta
3. OpenAI Memory API
4. LangChain Memory Module

### **Technical Documentation:**
1. Microsoft Semantic Kernel - https://learn.microsoft.com/semantic-kernel
2. Azure AI Search - https://learn.microsoft.com/azure/search
3. Clean Architecture (Robert C. Martin)
4. Domain-Driven Design (Eric Evans)

## **18.3 Team & Contributors**

**Project Lead:** Antonio Rapozo
- Senior Full-Stack Engineer
- 4+ years .NET / Azure experience
- Specialized in LLM applications and cognitive AI

**Open Source Contributors:** (To be added post-launch)

---

## **18.4 License**

```
MIT License

Copyright (c) 2025 Antonio Rapozo

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

# **DOCUMENT APPROVAL**

| Role | Name | Signature | Date |
|------|------|-----------|------|
| **Author** | Antonio Rapozo | _________________ | 2025-11-16 |
| **Tech Lead** | Antonio Rapozo | _________________ | 2025-11-16 |
| **Project Owner** | Antonio Rapozo | _________________ | 2025-11-16 |

---

**END OF DOCUMENT**

---

# **Quick Start Guide**

For developers ready to implement:

1. **Clone repository structure** (see Section 4.2)
2. **Install dependencies** (see Section 15.3)
3. **Set up Azure resources** (see Section 14.1)
4. **Follow Phase 1 roadmap** (see Section 16.1)
5. **Run tests** (see Section 13)
6. **Deploy** (see Section 14.2)

**Questions?** Open an issue on GitHub: `github.com/antoniorapozo/memorykit/issues`

---

**This TRD is a living document and will be updated as the project evolves.**