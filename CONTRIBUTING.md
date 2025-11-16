# Contributing to MemoryKit

First off, thank you for considering contributing to MemoryKit! It's people like you that make MemoryKit such a great tool.

## Code of Conduct

This project and everyone participating in it is governed by respect, professionalism, and inclusivity. By participating, you are expected to uphold these values.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples to demonstrate the steps**
- **Describe the behavior you observed and what you expected**
- **Include screenshots if applicable**
- **Include your environment details** (OS, .NET version, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, include:

- **Use a clear and descriptive title**
- **Provide a detailed description of the suggested enhancement**
- **Explain why this enhancement would be useful**
- **List any alternative solutions you've considered**

### Pull Requests

1. Fork the repo and create your branch from `main`
2. If you've added code that should be tested, add tests
3. Ensure the test suite passes
4. Make sure your code follows the existing code style
5. Write a clear commit message
6. Update documentation as needed

## Development Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/memorykit.git
cd memorykit

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Coding Standards

- Follow C# coding conventions and .NET best practices
- Use meaningful variable and method names
- Write XML documentation comments for public APIs
- Keep methods small and focused
- Write unit tests for new functionality
- Maintain or improve code coverage

## Project Structure

MemoryKit follows Clean Architecture:

- `src/MemoryKit.Domain/` - Core domain entities and interfaces
- `src/MemoryKit.Application/` - Application use cases and services
- `src/MemoryKit.Infrastructure/` - External service implementations
- `src/MemoryKit.API/` - REST API controllers and endpoints
- `tests/` - Unit and integration tests

## Commit Message Guidelines

- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests after the first line

Example:
```
Add episodic memory query optimization

- Implement caching layer for frequent queries
- Add index optimization for temporal searches
- Update documentation

Fixes #123
```

## Testing

- Write unit tests for all new functionality
- Ensure all tests pass before submitting a PR
- Aim for high code coverage
- Include both positive and negative test cases
- Test edge cases and error conditions

## Documentation

- Update README.md if you change functionality
- Update API documentation for new endpoints
- Add XML comments to public methods and classes
- Update relevant documentation in the `docs/` folder

## Questions?

Feel free to open an issue with your question or reach out to the maintainers.

Thank you for contributing to MemoryKit! 🧠
