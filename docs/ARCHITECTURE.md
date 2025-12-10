# Architecture Documentation

## Overview

MemoryKit implements a clean architecture with clear separation of concerns across four main layers.

## Layer Descriptions

### Domain Layer (MemoryKit.Domain)

The core business logic layer containing:

- **Entities**: Core domain objects (Message, Conversation, ExtractedFact, ProceduralPattern)
- **Value Objects**: Immutable objects representing values (ImportanceScore, EmbeddingVector, QueryPlan)
- **Enums**: Core domain enumerations (MessageRole, QueryType, MemoryLayer, EntityType, TriggerType)
- **Interfaces**: Contracts for external dependencies (no implementations)
- **Services**: Domain services with core business logic

Key principles:
- No external dependencies except logging abstractions
- Pure business logic, framework-agnostic
- Entities maintain invariants through private constructors and factory methods

### Application Layer (MemoryKit.Application)

Implements use cases and application logic:

- **Use Cases**: CQRS commands and queries (AddMessage, QueryMemory, GetContext)
- **DTOs**: Data transfer objects for API requests/responses
- **Validators**: FluentValidation rules for input validation
- **Mapping**: AutoMapper profiles for entity-to-DTO conversions
- **Services**: Orchestration and business process logic

Key principles:
- Depends on Domain layer
- Contains application-specific business logic
- Orchestrates interactions between layers

### Infrastructure Layer (MemoryKit.Infrastructure)

Implements external dependencies and technical concerns:

- **Azure**: Azure services (Redis, Table Storage, Blob, AI Search)
- **SemanticKernel**: Integration with Azure OpenAI and embeddings
- **Cognitive**: Neuroscience-inspired services (Amygdala, Hippocampus, Prefrontal Controller)
- **InMemory**: In-memory implementations for testing and development

Key principles:
- Depends on Domain and Application layers
- Implements interfaces defined in Domain
- Isolates external service dependencies

### Presentation Layer (MemoryKit.API)

ASP.NET Core Web API:

- **Controllers**: REST endpoints
- **Middleware**: Cross-cutting concerns
- **Filters**: Request/response processing
- **Program.cs**: Application configuration

Key principles:
- Depends on Application and Domain layers
- Handles HTTP protocol concerns
- Uses MediatR for dispatching commands/queries

## Memory Hierarchy

MemoryKit implements a four-layer memory system inspired by human cognition:

### Layer 3: Working Memory (Redis)
- **Latency**: < 5ms
- **Capacity**: 10 recent items per conversation
- **Purpose**: Hot context for active conversations
- **Service**: `IWorkingMemoryService`

### Layer 2: Semantic Memory (Table Storage)
- **Latency**: ~30ms
- **Capacity**: Unlimited (with pruning)
- **Purpose**: Extracted facts and entities
- **Service**: `IScratchpadService`

### Layer 1: Episodic Memory (Blob + AI Search)
- **Latency**: ~120ms
- **Capacity**: Full conversation history
- **Purpose**: Complete conversation archive
- **Service**: `IEpisodicMemoryService`

### Layer P: Procedural Memory (Table Storage)
- **Latency**: ~50ms
- **Capacity**: Learned patterns
- **Purpose**: Routines and preferences
- **Service**: `IProceduralMemoryService`

## Cognitive Model Mapping

| Brain Component | Function | Software | Service |
|---|---|---|---|
| Prefrontal Cortex | Executive function | Query planning | `IPrefrontalController` |
| Amygdala | Emotional tagging | Importance scoring | `IAmygdalaImportanceEngine` |
| Hippocampus | Short-term consolidation | Initial indexing | `IHippocampusIndexer` |
| Basal Ganglia | Procedural memory | Pattern matching | `IProceduralMemoryService` |

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

