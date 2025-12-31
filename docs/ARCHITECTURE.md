# Architecture Documentation

**Status:** Production Ready | **Version:** 1.0.0

---

## Quick Overview

MemoryKit uses **Clean Architecture** with strict dependency rules:

```
┌──────────────────────────────────────┐
│           API (REST)                 │
├──────────────────────────────────────┤
│       Application (Use Cases)        │
├──────────────────────────────────────┤
│    Domain (Business Logic) ⭐         │  ← No Dependencies!
├──────────────────────────────────────┤
│  Infrastructure (External Services)  │
└──────────────────────────────────────┘
```

**Key Rule:** Dependency flow is always inward. Domain has ZERO external dependencies.

## Layer Descriptions

### Domain Layer (MemoryKit.Domain)

**Purpose:** Core business logic - the heart of MemoryKit

| Component         | Examples                                                |
| ----------------- | ------------------------------------------------------- |
| **Entities**      | Message, Conversation, ExtractedFact, ProceduralPattern |
| **Value Objects** | ImportanceScore, EmbeddingVector, QueryPlan             |
| **Enums**         | MessageRole, QueryType, MemoryLayer, EntityType         |
| **Interfaces**    | All service contracts (IWorkingMemoryService, etc.)     |

**Rules:**

- ❌ ZERO external dependencies (except logging)
- ✅ Pure C# business logic
- ✅ Framework-agnostic
- ✅ Entities use factory methods, not public constructors

### Application Layer (MemoryKit.Application)

**Purpose:** Use cases and orchestration

| Component      | Technology       | Examples                              |
| -------------- | ---------------- | ------------------------------------- |
| **Use Cases**  | MediatR CQRS     | AddMessage, QueryMemory, GetContext   |
| **DTOs**       | Records          | CreateMessageRequest, MessageResponse |
| **Validators** | FluentValidation | CreateMessageRequestValidator         |
| **Mapping**    | AutoMapper       | Entity → DTO conversions              |

**Dependencies:** Domain only

### Infrastructure Layer (MemoryKit.Infrastructure)

**Purpose:** External service implementations

| Namespace          | Purpose                                       |
| ------------------ | --------------------------------------------- |
| **Azure**          | Redis, Table Storage, Blob Storage, AI Search |
| **Cognitive**      | Amygdala, Hippocampus, Prefrontal Controller  |
| **SemanticKernel** | Azure OpenAI, embeddings                      |
| **InMemory**       | Testing implementations                       |

**Dependencies:** Implements Domain interfaces

### API Layer (MemoryKit.API)

**Purpose:** REST endpoints

| Component       | Purpose                       |
| --------------- | ----------------------------- |
| **Controllers** | REST endpoints                |
| **Middleware**  | Rate limiting, authentication |
| **Program.cs**  | DI configuration              |

**Dependencies:** All layers (composition root)

## Memory Hierarchy

4-layer memory system inspired by human cognition:

| Layer              | Storage          | Latency | Capacity     | Purpose          |
| ------------------ | ---------------- | ------- | ------------ | ---------------- |
| **L3: Working**    | Redis            | <5ms    | 10 items     | Hot context      |
| **L2: Semantic**   | Table Storage    | ~30ms   | Unlimited\*  | Facts & entities |
| **L1: Episodic**   | Blob + AI Search | ~120ms  | Full history | Complete archive |
| **LP: Procedural** | Table Storage    | ~50ms   | Patterns     | Learned routines |

\*With intelligent pruning

**Services:**

- `IWorkingMemoryService`
- `IScratchpadService`
- `IEpisodicMemoryService`
- `IProceduralMemoryService`

## Cognitive Model Mapping

| Brain Component   | Function                 | Software           | Service                     |
| ----------------- | ------------------------ | ------------------ | --------------------------- |
| Prefrontal Cortex | Executive function       | Query planning     | `IPrefrontalController`     |
| Amygdala          | Emotional tagging        | Importance scoring | `IAmygdalaImportanceEngine` |
| Hippocampus       | Short-term consolidation | Initial indexing   | `IHippocampusIndexer`       |
| Basal Ganglia     | Procedural memory        | Pattern matching   | `IProceduralMemoryService`  |

## Data Flow

```
┌─────────────────┐
│  HTTP Request   │
└────────┬────────┘
         │
    ┌────▼─────┐
    │Controller │
    └────┬─────┘
         │
    ┌────▼──────────┐
    │ MediatR Query │
    └────┬──────────┘
         │
    ┌────▼──────────────┐
    │ Application Layer │
    └────┬──────────────┘
         │
    ┌────▼────────────────────────┐
    │ MemoryOrchestrator           │
    │ - BuildQueryPlan             │
    │ - RetrieveContext (parallel) │
    └────┬────────────────────────┘
         │
    ┌────┴──────────────────────────────┐
    │    (Parallel retrieval)            │
    │                                     │
 ┌──▼──┐ ┌──▼──┐ ┌──▼──┐ ┌──▼──┐      │
 │ L3  │ │ L2  │ │ L1  │ │ LP  │      │
 │Redis│ │Table│ │Blob+│ │Table│      │
 │     │ │Store│ │Search │      │      │
 └──┬──┘ └──┬──┘ └──┬──┘ └──┬──┘      │
    │       │       │       │         │
    └───────┴───────┴───────┴────────┘
         │
    ┌────▼──────────────┐
    │ SemanticKernel    │
    │ - LLM completion  │
    └────┬──────────────┘
         │
    ┌────▼─────────┐
    │ HTTP Response │
    └───────────────┘
```

## Extension Points

### Adding a New Memory Layer

1. Create interface in `Domain/Interfaces`
2. Implement `IMemoryLayer` interface
3. Register in dependency injection
4. Update `MemoryOrchestrator` to use new layer
5. Add tests

### Adding a New Cognitive Service

1. Create interface in `Infrastructure/Cognitive`
2. Implement service in `Infrastructure/Cognitive`
3. Register in dependency injection
4. Update relevant orchestrators
5. Add tests

### Adding a New API Endpoint

1. Create controller or update existing
2. Create mediator command/query
3. Create DTOs
4. Create validators
5. Add integration tests

## Patterns Used

### CQRS (Command Query Responsibility Segregation)

- Commands for write operations
- Queries for read operations
- Separated handlers for each

### Repository Pattern

- Abstracted data access
- Memory layer implementations

### Dependency Injection

- Loose coupling
- Testability
- Flexibility

### Factory Pattern

- Entity creation through factories
- Ensures valid state

### Strategy Pattern

- Pluggable memory layer implementations
- Different cognitive service strategies

## Performance Considerations

### Parallel Retrieval

- Memory layers retrieved in parallel
- Reduces total latency

### Caching Strategy

- Working memory LRU eviction
- Fact importance-based retention
- Episodic compression

### Token Optimization

- Minimal context selection
- Query plan optimization
- Irrelevant data filtering

## Security Considerations

- API authentication/authorization
- User data isolation
- GDPR-compliant deletion
- Input validation
- XSS prevention
- CSRF protection

## Testing Strategy

### Unit Tests

- Domain entities and value objects
- Business logic in services

### Integration Tests

- Memory layer interactions
- End-to-end query processing

### Performance Tests

- Latency monitoring
- Throughput testing

## Deployment Architecture

See `DEPLOYMENT.md` for cloud deployment strategies and infrastructure setup.

## Development Workflow

1. Start with Domain layer (entities, interfaces)
2. Implement Application layer (use cases)
3. Create Infrastructure implementations
4. Build API controllers
5. Write tests throughout
6. Performance testing
7. Security review
8. Documentation
