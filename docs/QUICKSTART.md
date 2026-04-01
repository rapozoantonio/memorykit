# MemoryKit - Quick Start Guide

Welcome to MemoryKit! This guide will get you up and running in minutes.

## Prerequisites

- .NET 9.0 SDK ([download](https://dotnet.microsoft.com/download))
- Visual Studio 2022, VS Code, or Rider
- Git

## Project Structure

```
MemoryKit/
├── src/                          # Source code
│   ├── MemoryKit.Domain/         # Core business logic
│   ├── MemoryKit.Application/    # Use cases & orchestration
│   ├── MemoryKit.Infrastructure/ # External services
│   └── MemoryKit.API/            # REST API
├── tests/                        # Unit & integration tests
├── samples/                      # Demo applications
├── docs/                         # Documentation
└── MemoryKit.sln                 # Solution file
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/antoniorapozo/memorykit.git
cd memorykit
```

### 2. Build the Solution

```bash
dotnet restore
dotnet build
```

### 3. Run Tests

```bash
dotnet test
```

### 4. Start the API

```bash
cd src/MemoryKit.API
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at `https://localhost:5001/swagger`.

## Key Concepts

### Memory Layers

MemoryKit uses a **4-layer memory hierarchy**:

1. **Working Memory (L3)**: Recent context, sub-5ms retrieval
2. **Semantic Memory (L2)**: Facts and entities, ~30ms retrieval
3. **Episodic Memory (L1)**: Full conversation history, ~120ms retrieval
4. **Procedural Memory (P)**: Learned patterns and routines

### Query Types

Different queries use different layers:

- **Continuation**: "Continue..." → L3 only
- **Fact Retrieval**: "What was..." → L2+L3
- **Deep Recall**: "Quote exactly..." → L1+L2+L3
- **Complex**: "Compare X and Y..." → All layers
- **Procedural**: "Write code..." → L3+P

### Architecture Layers

- **Domain**: Entities, interfaces, business rules
- **Application**: CQRS handlers, DTOs, validation
- **Infrastructure**: Azure services, LLM integration
- **API**: REST controllers, HTTP handling

## Common Tasks

### Creating a New Entity

In `Domain/Entities/`:

```csharp
public class MyEntity : Entity<string>
{
    public string Name { get; private set; }

    public static MyEntity Create(string name)
    {
        return new MyEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = name
        };
    }
}
```

### Adding a Use Case

Create in `Application/UseCases/{UseCaseName}/`:

```csharp
// Command/Query
public record MyCommand(string Data) : IRequest<MyResponse>;

// Handler
public class MyHandler : IRequestHandler<MyCommand, MyResponse>
{
    public async Task<MyResponse> Handle(MyCommand request, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Adding an API Endpoint

In `API/Controllers/`:

```csharp
[HttpPost("endpoint")]
public async Task<IActionResult> MyEndpoint(
    [FromBody] MyRequest request,
    CancellationToken ct)
{
    var command = new MyCommand(request.Data);
    var result = await _mediator.Send(command, ct);
    return Ok(result);
}
```

## Coding Standards

- Use **PascalCase** for classes and methods
- Use **camelCase** for parameters and private fields
- Add **XML documentation** to public members
- Follow **SOLID principles**
- Write **async** code for I/O operations
- Use **dependency injection** for services

Example:

```csharp
/// <summary>
/// Processes a command with optional timeout.
/// </summary>
/// <param name="command">The command to process</param>
/// <param name="timeout">Optional timeout in seconds</param>
/// <returns>The processing result</returns>
public async Task<Result> ProcessAsync(Command command, int? timeout = null)
{
    // Implementation
}
```

## Testing

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/MemoryKit.Domain.Tests

# With coverage
dotnet test /p:CollectCoverage=true

# Watch mode
dotnet test --watch
```

### Writing Tests

Use xUnit and name tests descriptively:

```csharp
[Fact]
public async Task RetrieveContext_WithValidQuery_ReturnsMemoryContext()
{
    // Arrange
    var query = "test query";

    // Act
    var result = await _service.RetrieveAsync(query);

    // Assert
    Assert.NotNull(result);
}
```

## Debugging

### In Visual Studio

1. Set breakpoints
2. Press F5 or Debug → Start Debugging
3. Use Debug Console for inspection

### In VS Code

1. Install C# Dev Kit
2. Create `.vscode/launch.json` (comes with extension)
3. Press F5

### Common Issues

**Problem**: Port 5001 already in use

```bash
dotnet run --urls "https://localhost:5002"
```

**Problem**: NuGet package restore fails

```bash
dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org
dotnet restore
```

**Problem**: Build fails with SDK version

```bash
dotnet --version
# Update to .NET 9.0 if needed
```

## Database & Storage

MemoryKit supports **two storage providers**:

### In-Memory (Default)

No setup required.

```json
{
  "MemoryKit": {
    "StorageProvider": "InMemory"
  }
}
```

### Azure (Production)

Enterprise-grade persistent storage with automatic failover.

**Required Resources:**

- Azure Cache for Redis (Working Memory)
- Azure Storage Account (Semantic/Procedural/Episodic)
- Azure AI Search (Vector search)

**Configuration:**

```json
{
  "MemoryKit": {
    "StorageProvider": "Azure",
    "Azure": {
      "RedisConnectionString": "${AZURE_REDIS_CONNECTION_STRING}",
      "StorageConnectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
      "SearchEndpoint": "${AZURE_SEARCH_ENDPOINT}",
      "SearchApiKey": "${AZURE_SEARCH_API_KEY}"
    }
  }
}
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed Azure setup.

## Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md): Deep dive into system design
- [API.md](API.md): REST API reference
- [DEPLOYMENT.md](DEPLOYMENT.md): Azure deployment guide
- [COGNITIVE_MODEL.md](COGNITIVE_MODEL.md): Neuroscience inspiration

## Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/my-feature`
3. Make changes and commit: `git commit -m "Add my feature"`
4. Push: `git push origin feature/my-feature`
5. Create Pull Request

See [CONTRIBUTING.md](../CONTRIBUTING.md) for detailed guidelines.

## Resources

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Azure Documentation](https://docs.microsoft.com/en-us/azure/)

## Support

- 📖 Check documentation in [/docs](.)
- 🐛 Report bugs on GitHub Issues
- 💬 Ask questions in Discussions
- 📧 Contact maintainers

## License

MIT License - see [LICENSE](../LICENSE) file

---

**Ready to contribute?** See [CONTRIBUTING.md](../CONTRIBUTING.md) to get started! 🚀
