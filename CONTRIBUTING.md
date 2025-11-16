# Contributing to MemoryKit

Thank you for your interest in contributing to MemoryKit! This document provides guidelines and instructions for contributing.

## Code of Conduct

We are committed to providing a welcoming and inspiring community for all. Please read and adhere to our Code of Conduct.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the issue list as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

* **Use a clear and descriptive title**
* **Describe the exact steps which reproduce the problem**
* **Provide specific examples to demonstrate the steps**
* **Describe the behavior you observed after following the steps**
* **Describe the expected behavior**
* **Include screenshots and animated GIFs if possible**
* **Include your environment details**

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

* **Use a clear and descriptive title**
* **Provide a step-by-step description of the suggested enhancement**
* **Provide specific examples to demonstrate the steps**
* **Describe the current behavior and the expected behavior**
* **Explain why this enhancement would be useful**

### Pull Requests

* Fill in the required template
* Follow the C# styleguides
* Include appropriate test cases
* End all files with a newline
* Update documentation accordingly

## Development Setup

### Prerequisites

* .NET 9.0 SDK or later
* Visual Studio 2022, Visual Studio Code, or JetBrains Rider
* Git

### Getting Started

1. Fork the repository
2. Clone your fork locally:
   ```bash
   git clone https://github.com/your-username/memorykit.git
   cd memorykit
   ```
3. Add upstream remote:
   ```bash
   git remote add upstream https://github.com/antoniorapozo/memorykit.git
   ```
4. Create a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Code Style

We follow the C# coding guidelines and conventions. Key points:

* Use meaningful names for variables, methods, and classes
* Follow SOLID principles
* Write clean, readable code
* Add XML documentation comments to public members
* Use async/await for I/O operations

Example:

```csharp
/// <summary>
/// Retrieves context for a given query.
/// </summary>
public async Task<MemoryContext> RetrieveContextAsync(
    string userId,
    string conversationId,
    string query,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

## Commit Messages

* Use the present tense ("Add feature" not "Added feature")
* Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
* Limit the first line to 72 characters or less
* Reference issues and pull requests liberally after the first line

Example:

```
Add support for semantic pattern matching

- Implement semantic trigger type in ProceduralPattern
- Add vector-based pattern matching logic
- Update tests for new functionality

Closes #123
```

## Branch Naming

Use the following branch naming convention:

* `feature/description-of-feature` - for new features
* `bugfix/description-of-bug` - for bug fixes
* `docs/description-of-docs` - for documentation changes
* `refactor/description-of-refactor` - for refactoring

## Pull Request Process

1. Update the README.md with any new features or significant changes
2. Update the CHANGELOG.md with notes on your changes
3. Ensure all tests pass: `dotnet test`
4. Ensure code builds without warnings: `dotnet build /warnaserror`
5. Create a pull request with a clear description

## Additional Notes

### Architecture

Please familiarize yourself with the Clean Architecture approach used in this project:

* **Domain Layer**: Core business logic and entities
* **Application Layer**: Use cases, CQRS, validation
* **Infrastructure Layer**: External service implementations
* **Presentation Layer**: API controllers and HTTP handling

### Testing

We aim for high test coverage. Please include tests for:

* New features
* Bug fixes
* Edge cases

Test naming convention: `{MethodName}_{Scenario}_{Expected}`

Example: `RetrieveContext_WithValidQuery_ReturnsMemoryContext`

## Recognition

Contributors will be recognized in:
* The project README
* Release notes
* GitHub contributors page

## Questions?

Feel free to open an issue with your questions or contact the maintainers.

Thank you for contributing to MemoryKit! 🎉
