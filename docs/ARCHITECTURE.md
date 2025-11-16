# MemoryKit Architecture

## Overview

MemoryKit follows Clean Architecture principles, ensuring separation of concerns, testability, and maintainability.

## Architecture Layers

### 1. Domain Layer (`MemoryKit.Domain`)

The innermost layer containing core business logic and domain entities.

**Key Components:**

- **Entities**: Core business objects (Message, Conversation, ExtractedFact, ProceduralPattern)
- **Value Objects**: Immutable objects representing concepts (ImportanceScore, EmbeddingVector, QueryPlan)
- **Enums**: Type definitions (QueryType, EntityType, MemoryLayer)
- **Interfaces**: Contracts for external dependencies
- **Domain Services**: Business logic that doesn't fit in entities

**Dependencies**: None (pure domain logic)

### 2. Application Layer (`MemoryKit.Application`)

Contains application-specific business rules and orchestrates domain objects.

**Key Components:**

- **Use Cases**: Application workflows (AddMessage, QueryMemory, GetContext)
- **DTOs**: Data Transfer Objects for cross-layer communication
- **Mapping**: Object-to-object mapping logic
- **Validators**: Input validation rules
- **Services**: Application-level orchestration (ConversationManager, MemoryOrchestrator, PrefrontalController)

**Dependencies**: Domain Layer

### 3. Infrastructure Layer (`MemoryKit.Infrastructure`)

Implements external concerns and interfaces defined in the domain.

**Key Components:**

- **Azure Services**: Cloud implementations (Redis, Table Storage, Blob Storage, AI Search)
- **Semantic Kernel**: AI integration for query classification and entity extraction
- **Cognitive Services**: Importance engine and indexing (AmygdalaImportanceEngine, HippocampusIndexer)
- **In-Memory**: Test implementations

**Dependencies**: Domain Layer, Application Layer

### 4. API Layer (`MemoryKit.API`)

Exposes the application via REST endpoints.

**Key Components:**

- **Controllers**: HTTP endpoints (ConversationsController, MemoriesController, PatternsController)
- **Middleware**: Cross-cutting concerns
- **Filters**: Request/response processing
- **Program.cs**: Application entry point and configuration

**Dependencies**: All layers

## Cognitive Memory Model

MemoryKit is inspired by human memory systems:

### Working Memory (Short-term)
- **Implementation**: Redis
- **Purpose**: Active conversation context
- **Capacity**: Limited (last N messages)
- **Lifetime**: Session-based

### Episodic Memory (Long-term)
- **Implementation**: Azure Blob Storage + AI Search
- **Purpose**: Complete conversation episodes
- **Capacity**: Unlimited
- **Lifetime**: Persistent

### Semantic Memory (Long-term)
- **Implementation**: Azure AI Search
- **Purpose**: Extracted facts and knowledge
- **Capacity**: Unlimited
- **Lifetime**: Persistent

### Procedural Memory (Long-term)
- **Implementation**: Azure Table Storage
- **Purpose**: Learned patterns and behaviors
- **Capacity**: Unlimited
- **Lifetime**: Persistent

## Data Flow

```
User Request
    ↓
API Controller
    ↓
Use Case Handler
    ↓
Domain Services
    ↓
Infrastructure Services
    ↓
External Storage (Azure/Redis)
```

## Key Design Patterns

- **CQRS**: Separate read and write operations
- **Repository Pattern**: Abstract data access
- **Dependency Injection**: Loose coupling
- **Strategy Pattern**: Pluggable memory implementations
- **Factory Pattern**: Object creation
- **Mediator Pattern**: Request handling (via MediatR)

## Scalability Considerations

- **Horizontal Scaling**: Stateless API design
- **Caching**: Redis for working memory
- **Partitioning**: Azure services support sharding
- **Async Operations**: Non-blocking I/O
- **Event-Driven**: Future support for event sourcing

## Security

- **Authentication**: Azure AD integration ready
- **Authorization**: Role-based access control
- **Data Encryption**: At rest and in transit
- **Input Validation**: All endpoints validated
- **Rate Limiting**: API throttling support

## Testing Strategy

- **Unit Tests**: Domain and application logic
- **Integration Tests**: Infrastructure implementations
- **API Tests**: End-to-end controller testing
- **Performance Tests**: Load and stress testing

## Future Enhancements

- Event sourcing for full audit trail
- Multi-tenancy support
- Advanced query optimization
- Real-time memory consolidation
- Federated learning for procedural memory
