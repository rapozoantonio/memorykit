# Contributing to MemoryKit

**First time here?** Jump to [Quick Start](#quick-start) to make your first contribution in 5 minutes.

---

## Quick Start

```bash
# 1. Fork and clone
git clone https://github.com/YOUR-USERNAME/memorykit.git
cd memorykit

# 2. Build and test
dotnet restore && dotnet build
dotnet test

# 3. Make changes and submit
git checkout -b feature/your-feature
# ... make your changes ...
git push origin feature/your-feature
# Open PR on GitHub
```

**That's it!** See below for detailed guidelines.

---

## How to Contribute

### Report a Bug

**Before reporting:** Search [existing issues](https://github.com/rapozoantonio/memorykit/issues) to avoid duplicates.

**Required information:**

| Field                  | Description                      |
| ---------------------- | -------------------------------- |
| **Title**              | Clear, descriptive summary       |
| **Steps to reproduce** | Exact steps (numbered list)      |
| **Expected behavior**  | What should happen               |
| **Actual behavior**    | What actually happens            |
| **Environment**        | OS, .NET version, Docker version |
| **Screenshots**        | If applicable                    |

**Use our [bug report template](https://github.com/rapozoantonio/memorykit/issues/new?template=bug_report.md)** for guidance.

### Suggest a Feature

Use our [feature request template](https://github.com/rapozoantonio/memorykit/issues/new?template=feature_request.md).

**Include:**

- Problem it solves
- Proposed solution
- Use cases
- Example API (if applicable)

### Submit a Pull Request

**Checklist:**

- [ ] Tests pass: `dotnet test`
- [ ] No warnings: `dotnet build /warnaserror`
- [ ] CHANGELOG.md updated
- [ ] Documentation updated (if needed)
- [ ] PR template filled out

See [Pull Request Process](#pull-request-process) for details.

## Development Setup

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Code editor (VS 2022 / VS Code / Rider)
- Git

### Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~YourTestName"

# Build without warnings
dotnet build /warnaserror
```

### Code Style

| Rule              | Example                                                  |
| ----------------- | -------------------------------------------------------- |
| **Naming**        | PascalCase for classes/methods, camelCase for parameters |
| **Async**         | Always suffix with `Async`, use `CancellationToken`      |
| **Documentation** | XML comments on all public members                       |
| **Architecture**  | Keep Domain layer pure (no external dependencies)        |
| **SOLID**         | Follow SOLID principles                                  |

**Example:**

```csharp
/// <summary>
/// Retrieves context for a given query.
/// </summary>
/// <param name="query">The user's query text</param>
/// <param name="cancellationToken">Cancellation token</param>
public async Task<MemoryContext> RetrieveContextAsync(
    string query,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

## Commit Messages

**Format:** `<type>(<scope>): <subject>`

| Type       | Use For                 |
| ---------- | ----------------------- |
| `feat`     | New feature             |
| `fix`      | Bug fix                 |
| `docs`     | Documentation           |
| `test`     | Tests                   |
| `refactor` | Code refactoring        |
| `perf`     | Performance improvement |

**Example:**

```
feat(memory): add semantic pattern matching

- Implement semantic trigger type in ProceduralPattern
- Add vector-based pattern matching logic
- Update tests for new functionality

Closes #123
```

## Branch Naming

Use the following branch naming convention:

- `feature/description-of-feature` - for new features
- `bugfix/description-of-bug` - for bug fixes
- `docs/description-of-docs` - for documentation changes
- `refactor/description-of-refactor` - for refactoring

## Pull Request Process

1. Update the README.md with any new features or significant changes
2. Update the CHANGELOG.md with notes on your changes
3. Ensure all tests pass: `dotnet test`
4. Ensure code builds without warnings: `dotnet build /warnaserror`
5. Create a pull request with a clear description

## Architecture Guide

MemoryKit uses **Clean Architecture** with strict dependency rules:

```
API → Application → Domain ← Infrastructure
```

| Layer              | Purpose              | Dependencies |
| ------------------ | -------------------- | ------------ |
| **Domain**         | Core logic, entities | ❌ None      |
| **Application**    | Use cases, CQRS      | Domain only  |
| **Infrastructure** | Azure, Redis, AI     | Domain only  |
| **API**            | REST endpoints       | All layers   |

**Key Rule:** Domain has ZERO external dependencies.

📖 See [DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md) for details.

## Testing

**Naming:** `{MethodName}_{Scenario}_{Expected}`

**Example:** `RetrieveContext_WithValidQuery_ReturnsMemoryContext`

**Test types:**

- Unit tests: Test individual components
- Integration tests: Test component interactions
- Benchmark tests: Performance validation

**Coverage:** Aim for >80% on new code.

## Pull Request Process

1. **Branch:** Create from `main` using naming convention below
2. **Code:** Make changes, add tests, update docs
3. **Verify:** Run `dotnet test` and `dotnet build /warnaserror`
4. **Commit:** Follow commit message format above
5. **Push:** Push to your fork
6. **PR:** Create PR with template, link issues

### Branch Naming

| Type | Format | Example |\n|------|--------|--------|\n| Feature | `feature/description` | `feature/semantic-search` |\n| Bug fix | `bugfix/description` | `bugfix/memory-leak` |\n| Docs | `docs/description` | `docs/api-examples` |\n| Refactor | `refactor/description` | `refactor/query-engine` |\n\n## Recognition\n\nContributors are listed in:\n- [README.md](README.md) Contributors section\n- Release notes\n- GitHub contributors page\n\n---\n\n## Need Help?\n\n- \ud83d\udc1b [Report a bug](https://github.com/rapozoantonio/memorykit/issues/new?template=bug_report.md)\n- \ud83d\udca1 [Request a feature](https://github.com/rapozoantonio/memorykit/issues/new?template=feature_request.md)\n- \ud83d\udcac [Join discussions](https://github.com/rapozoantonio/memorykit/discussions)\n- \ud83d\udcda [Read the docs](docs/README.md)\n\n**Thank you for contributing to MemoryKit!** \ud83c\udf89
