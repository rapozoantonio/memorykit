# 🛠️ MemoryKit Development Guide

**Version:** 1.0.0
**Last Updated:** December 26, 2024
**Status:** Production-Ready

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Getting Started](#getting-started)
4. [Development Workflow](#development-workflow)
5. [Code Quality Standards](#code-quality-standards)
6. [Testing Strategy](#testing-strategy)
7. [Debugging & Troubleshooting](#debugging--troubleshooting)
8. [Deployment](#deployment)
9. [Contributing Guidelines](#contributing-guidelines)

---

## Architecture Overview

MemoryKit follows **Clean Architecture** principles with strict dependency rules:

```
┌─────────────────────────────────────────────────────┐
│                    Presentation                     │
│            MemoryKit.API (Controllers)              │
└────────────────────┬────────────────────────────────┘
                     │ depends on ↓
┌────────────────────▼────────────────────────────────┐
│                   Application                       │
│     MemoryKit.Application (Use Cases, DTOs)        │
└────────────────────┬────────────────────────────────┘
                     │ depends on ↓
┌────────────────────▼────────────────────────────────┐
│                     Domain                          │
│   MemoryKit.Domain (Entities, Interfaces, Logic)   │
│              ⚠️ NO DEPENDENCIES ⚠️                  │
└────────────────────▲────────────────────────────────┘
                     │ implements ↑
┌────────────────────┴────────────────────────────────┐
│                 Infrastructure                      │
│  MemoryKit.Infrastructure (Azure, Redis, SK, etc)  │
└─────────────────────────────────────────────────────┘
```

### Dependency Rules (CRITICAL)

✅ **ALLOWED**:

- Domain → (nothing - zero dependencies)
- Application → Domain
- Infrastructure → Domain
- API → Application + Infrastructure + Domain

❌ **FORBIDDEN**:

- Domain → anything else
- Application → Infrastructure (use interfaces from Domain)
- Infrastructure → Application

**Architectural Decision**: All service interfaces are defined in `Domain.Interfaces` to eliminate circular dependencies and maintain clean architecture principles.

---

## Project Structure

```
MemoryKit/
├── src/
│   ├── MemoryKit.Domain/             # Core business logic
│   │   ├── Entities/                 # Domain entities
│   │   │   ├── Conversation.cs
│   │   │   ├── Message.cs
│   │   │   ├── ExtractedFact.cs
│   │   │   └── ProceduralPattern.cs
│   │   ├── Enums/                    # Domain enums
│   │   │   └── DomainEnums.cs        # MessageRole, MemoryLayer, etc.
│   │   ├── Interfaces/               # ⚠️ ALL INTERFACES HERE
│   │   │   └── DomainInterfaces.cs   # Service interfaces
│   │   ├── ValueObjects/             # Immutable value types
│   │   │   └── ValueObjects.cs       # ImportanceScore, QueryPlan, etc.
│   │   └── Common/
│   │       └── Entity.cs             # Base entity class
│   │
│   ├── MemoryKit.Application/        # Use cases & orchestration
│   │   ├── Services/
│   │   │   ├── MemoryOrchestrator.cs       # Central coordinator
│   │   │   ├── AmygdalaImportanceEngine.cs # Importance scoring
│   │   │   └── PrefrontalController.cs     # Query planning
│   │   ├── UseCases/
│   │   │   ├── AddMessage/                 # CQRS commands
│   │   │   ├── GetContext/                 # CQRS queries
│   │   │   └── QueryMemory/
│   │   ├── DTOs/                           # Data transfer objects
│   │   └── Validators/                     # FluentValidation
│   │
│   ├── MemoryKit.Infrastructure/     # External concerns
│   │   ├── Cognitive/                # Cognitive implementations
│   │   │   ├── AmygdalaImportanceEngineService.cs
│   │   │   ├── PrefrontalControllerService.cs
│   │   │   └── HippocampusIndexer.cs
│   │   ├── InMemory/                 # In-memory implementations
│   │   │   ├── InMemoryWorkingMemoryService.cs
│   │   │   ├── InMemoryScratchpadService.cs
│   │   │   └── InMemoryEpisodicMemoryService.cs
│   │   ├── SemanticKernel/           # Azure OpenAI integration
│   │   │   ├── SemanticKernelService.cs
│   │   │   └── MockSemanticKernelService.cs
│   │   └── Azure/                    # Future: Azure implementations
│   │       └── [Azure production services available]
│   │
│   └── MemoryKit.API/                # Web API
│       ├── Controllers/
│       │   ├── ConversationsController.cs
│       │   ├── MemoriesController.cs
│       │   └── PatternsController.cs
│       ├── Authentication/           # API key auth
│       ├── HealthChecks/             # Health endpoints
│       ├── Program.cs                # Startup & DI
│       └── appsettings.json
│
├── tests/
│   ├── MemoryKit.Domain.Tests/
│   ├── MemoryKit.Application.Tests/
│   ├── MemoryKit.Infrastructure.Tests/
│   ├── MemoryKit.API.Tests/
│   ├── MemoryKit.IntegrationTests/
│   └── MemoryKit.Benchmarks/         # Performance benchmarks
│
├── docs/
│   ├── ARCHITECTURE.md               # Technical architecture
│   ├── API.md                        # API documentation
│   ├── COGNITIVE_MODEL.md            # Neuroscience mappings
│   ├── DEPLOYMENT.md                 # Deployment guide
│   ├── PRODUCTION_HARDENING.md       # Production checklist
│   └── SCIENTIFIC_OVERVIEW.md        # Research documentation
│
└── samples/
    ├── MemoryKit.ConsoleDemo/        # CLI demo
    └── MemoryKit.BlazorDemo/         # Web UI demo
```

---

## Getting Started

### Prerequisites

- **.NET 9.0 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **Visual Studio 2022** (17.8+) or **Visual Studio Code** with C# extension
- **Azure Account** (optional, for production Azure services)
- **Git** for version control

### First-Time Setup

1. **Clone the repository**:

   ```bash
   git clone https://github.com/rapozoantonio/memorykit.git
   cd memorykit
   ```

2. **Restore dependencies**:

   ```bash
   dotnet restore
   ```

3. **Build the solution**:

   ```bash
   dotnet build
   ```

4. **Run tests**:

   ```bash
   dotnet test
   ```

5. **Run the API**:

   ```bash
   cd src/MemoryKit.API
   dotnet run
   ```

6. **Access Swagger UI**: Navigate to `https://localhost:5001/swagger`

### Configuration

Create `appsettings.Development.json` in `src/MemoryKit.API/`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "MemoryKit": {
    "UseInMemoryServices": true
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4",
    "EmbeddingDeployment": "text-embedding-ada-002"
  },
  "ApiKeys": ["dev-test-key-12345"]
}
```

---

## Development Workflow

### 1. Feature Branch Workflow

```bash
# Create feature branch
git checkout -b feature/your-feature-name

# Make changes
# ... code ...

# Commit with conventional commits
git commit -m "feat: add procedural pattern matching"
git commit -m "fix: resolve circular dependency in DI"
git commit -m "docs: update API documentation"

# Push and create PR
git push origin feature/your-feature-name
```

### 2. Conventional Commits

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation only
- `style:` Code style (formatting, no logic change)
- `refactor:` Code refactoring
- `perf:` Performance improvement
- `test:` Adding or updating tests
- `chore:` Maintenance tasks

### 3. Code Review Checklist

Before submitting PR, ensure:

- [ ] All tests pass (`dotnet test`)
- [ ] Code follows C# conventions
- [ ] XML documentation on all public APIs
- [ ] No circular dependencies
- [ ] Logging added for important operations
- [ ] Error handling implemented
- [ ] Performance considered (avoid N+1 queries)

---

## Code Quality Standards

### C# Coding Conventions

```csharp
// ✅ GOOD: Descriptive names, async all the way
public async Task<MemoryContext> RetrieveContextAsync(
    string userId,
    string conversationId,
    string query,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation(
        "Retrieving context for user {UserId}, conversation {ConversationId}",
        userId,
        conversationId);

    // Validate inputs
    ArgumentException.ThrowIfNullOrEmpty(userId);
    ArgumentException.ThrowIfNullOrEmpty(conversationId);

    try
    {
        var plan = await _prefrontal.BuildQueryPlanAsync(query, state, cancellationToken);
        return await ExecutePlanAsync(plan, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve context for user {UserId}", userId);
        throw;
    }
}

// ❌ BAD: Sync over async, no logging, poor error handling
public MemoryContext GetContext(string user, string conv, string q)
{
    var plan = _prefrontal.BuildQueryPlanAsync(q, null).Result; // ❌ .Result blocks
    return ExecutePlanAsync(plan).Result;
}
```

### Naming Conventions

| Type         | Convention          | Example                      |
| ------------ | ------------------- | ---------------------------- |
| Interface    | `I` prefix          | `IMemoryOrchestrator`        |
| Service      | `Service` suffix    | `SemanticKernelService`      |
| Controller   | `Controller` suffix | `ConversationsController`    |
| DTO          | Descriptive noun    | `QueryMemoryRequest`         |
| Enum         | Singular noun       | `MessageRole`, `MemoryLayer` |
| Async method | `Async` suffix      | `RetrieveContextAsync`       |

### XML Documentation (Required)

```csharp
/// <summary>
/// Retrieves and assembles memory context for a given query.
/// Implements intelligent layer selection based on query classification.
/// </summary>
/// <param name="userId">The unique identifier for the user.</param>
/// <param name="conversationId">The conversation identifier.</param>
/// <param name="query">The user's query text.</param>
/// <param name="cancellationToken">Cancellation token for async operation.</param>
/// <returns>
/// A <see cref="MemoryContext"/> containing relevant memories from appropriate layers.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
/// <exception cref="MemoryRetrievalException">Thrown when memory retrieval fails.</exception>
public async Task<MemoryContext> RetrieveContextAsync(...)
```

### Dependency Injection

✅ **Constructor Injection (Preferred)**:

```csharp
public class MemoryOrchestrator : IMemoryOrchestrator
{
    private readonly IWorkingMemoryService _workingMemory;
    private readonly ILogger<MemoryOrchestrator> _logger;

    public MemoryOrchestrator(
        IWorkingMemoryService workingMemory,
        ILogger<MemoryOrchestrator> logger)
    {
        _workingMemory = workingMemory ?? throw new ArgumentNullException(nameof(workingMemory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

❌ **Service Locator (Avoid)**:

```csharp
// Don't do this
var service = serviceProvider.GetService<IWorkingMemoryService>();
```

---

## Testing Strategy

### Test Pyramid

```
           /\
          /  \         10% → E2E Tests (IntegrationTests)
         /────\
        /      \       30% → Integration Tests (Infrastructure.Tests)
       /────────\
      /          \     60% → Unit Tests (Domain.Tests, Application.Tests)
     /────────────\
    /______________\
```

### Unit Tests Example

```csharp
public class AmygdalaImportanceEngineTests
{
    [Fact]
    public async Task CalculateImportance_WithExplicitMarkers_ReturnsHighScore()
    {
        // Arrange
        var logger = new NullLogger<AmygdalaImportanceEngine>();
        var engine = new AmygdalaImportanceEngine(logger);

        var message = new Message
        {
            Id = "test-123",
            Content = "IMPORTANT: Remember to deploy the fix",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var score = await engine.CalculateImportanceAsync(message);

        // Assert
        Assert.True(score.FinalScore > 0.7, "Messages with explicit markers should have high importance");
        Assert.Equal(MessageRole.User, message.Role);
    }

    [Theory]
    [InlineData("what is the weather?", 0.5, 0.8)]  // Question gets boost
    [InlineData("i will deploy tomorrow", 0.7, 1.0)]  // Decision language
    [InlineData("hello world", 0.3, 0.6)]  // Simple message
    public async Task CalculateImportance_VariousInputs_ReturnsExpectedRange(
        string content,
        double minExpected,
        double maxExpected)
    {
        // Arrange & Act & Assert
        var message = new Message { Content = content, Role = MessageRole.User, Timestamp = DateTime.UtcNow };
        var engine = new AmygdalaImportanceEngine(new NullLogger<AmygdalaImportanceEngine>());
        var score = await engine.CalculateImportanceAsync(message);

        Assert.InRange(score.FinalScore, minExpected, maxExpected);
    }
}
```

### Integration Tests

```csharp
public class MemoryOrchestratorIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MemoryOrchestratorIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RetrieveContext_AfterStoringMessage_ReturnsStoredContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = "test-user";
        var conversationId = Guid.NewGuid().ToString();

        // Act: Store message
        var storeRequest = new
        {
            userId,
            conversationId,
            message = new
            {
                role = "User",
                content = "My favorite color is blue"
            }
        };

        var storeResponse = await client.PostAsJsonAsync("/api/memories", storeRequest);
        storeResponse.EnsureSuccessStatusCode();

        // Wait for consolidation
        await Task.Delay(100);

        // Act: Retrieve context
        var queryRequest = new { question = "What is my favorite color?" };
        var queryResponse = await client.PostAsJsonAsync(
            $"/api/memories/{userId}/{conversationId}/query",
            queryRequest);

        // Assert
        var result = await queryResponse.Content.ReadFromJsonAsync<QueryMemoryResponse>();
        Assert.Contains("blue", result.Answer, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/MemoryKit.Domain.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Run benchmarks
dotnet run --project tests/MemoryKit.Benchmarks --configuration Release
```

---

## Debugging & Troubleshooting

### Common Issues

#### 1. Circular Dependency Error

**Error**: `A circular dependency was detected`

**Cause**: Application layer referencing Infrastructure types directly.

**Solution**: Always use interfaces from `Domain.Interfaces`:

```csharp
// ❌ Wrong
using MemoryKit.Infrastructure.Azure;

// ✅ Correct
using MemoryKit.Domain.Interfaces;
```

#### 2. "Service not registered" DI Error

**Error**: `Unable to resolve service for type 'IWorkingMemoryService'`

**Solution**: Register in `Program.cs`:

```csharp
builder.Services.AddScoped<IWorkingMemoryService, InMemoryWorkingMemoryService>();
```

#### 3. Null Reference in Async Code

**Symptom**: `NullReferenceException` in async methods

**Common Cause**: Not awaiting tasks properly

```csharp
// ❌ Wrong - deadlock risk
var result = SomeAsyncMethod().Result;

// ✅ Correct
var result = await SomeAsyncMethod();
```

### Logging Best Practices

```csharp
// ✅ Structured logging with parameters
_logger.LogInformation(
    "Retrieving context for user {UserId}, conversation {ConversationId}",
    userId,
    conversationId);

// ❌ String interpolation (poor performance, no structured data)
_logger.LogInformation($"Retrieving context for user {userId}");

// ✅ Log exceptions with context
_logger.LogError(
    ex,
    "Failed to retrieve context for user {UserId}",
    userId);
```

### Performance Profiling

Use BenchmarkDotNet for micro-benchmarks:

```csharp
[MemoryDiagnoser]
public class MemoryRetrievalBenchmarks
{
    [Benchmark]
    public async Task RetrieveFromWorkingMemory()
    {
        await _orchestrator.RetrieveContextAsync("user1", "conv1", "test query");
    }
}
```

---

## Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed deployment instructions.

### Quick Deploy to Azure

```bash
# Login to Azure
az login

# Create resource group
az group create --name memorykit-rg --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group memorykit-rg \
  --template-file infrastructure/main.bicep \
  --parameters @infrastructure/parameters.prod.json

# Deploy application
dotnet publish -c Release
az webapp deploy --resource-group memorykit-rg --name memorykit-api --src-path ./publish.zip
```

---

## Contributing Guidelines

### How to Contribute

1. **Fork the repository** on GitHub
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Make your changes** following code quality standards
4. **Add tests** for new functionality
5. **Update documentation** if needed
6. **Commit with conventional commits**: `git commit -m 'feat: add amazing feature'`
7. **Push to branch**: `git push origin feature/amazing-feature`
8. **Open a Pull Request** with clear description

### PR Review Process

1. Automated checks must pass:
   - ✅ All tests pass
   - ✅ No build warnings
   - ✅ Code coverage > 80%

2. Code review by maintainer:
   - Architecture adherence
   - Code quality and readability
   - Performance implications

3. Merge requirements:
   - 1 approval from maintainer
   - All conversations resolved
   - Up-to-date with main branch

### Areas for Contribution

- [ ] Azure production implementations (Redis, Blob, Table Storage)
- [ ] Additional LLM providers (OpenAI, Anthropic, Google)
- [ ] Multi-modal memory (image, audio)
- [ ] Performance optimizations
- [ ] Documentation improvements
- [ ] Sample applications
- [ ] Client SDKs (Python, JavaScript)

---

## Resources

- **GitHub Repository**: https://github.com/rapozoantonio/memorykit
- **Documentation**: [README.md](README.md)
- **Issue Tracker**: https://github.com/rapozoantonio/memorykit/issues
- **Discussions**: https://github.com/rapozoantonio/memorykit/discussions

---

## License

MIT License - see [LICENSE](../LICENSE) for details.

---

**Questions?** Open an issue or start a discussion on GitHub!
