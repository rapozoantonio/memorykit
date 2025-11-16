# Documentation

## Contents

This directory contains comprehensive documentation for MemoryKit:

- **ARCHITECTURE.md** - Clean architecture overview, layer descriptions, and design patterns
- **API.md** - REST API endpoints, usage examples, and SDK documentation
- **DEPLOYMENT.md** - Azure deployment guide, infrastructure setup, and scaling configuration
- **COGNITIVE_MODEL.md** - Neuroscience-inspired architecture explanation

## Quick Links

- [Main README](../README.md)
- [Contributing Guidelines](../CONTRIBUTING.md)
- [API Documentation](./API.md)

## Building the Project

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Publish
dotnet publish src/MemoryKit.API/MemoryKit.API.csproj -c Release
```

## Architecture Highlights

MemoryKit implements a **clean architecture** with:

- **Domain Layer**: Core business logic and entities
- **Application Layer**: Use cases, CQRS, and validation
- **Infrastructure Layer**: External services (Azure, OpenAI, etc.)
- **Presentation Layer**: ASP.NET Core Web API

## Getting Started

1. Review [ARCHITECTURE.md](./ARCHITECTURE.md) for system design
2. Check [API.md](./API.md) for available endpoints
3. Follow [DEPLOYMENT.md](./DEPLOYMENT.md) to deploy to Azure
4. Read [COGNITIVE_MODEL.md](./COGNITIVE_MODEL.md) to understand the neuroscience inspiration

## For Contributors

- See [CONTRIBUTING.md](../CONTRIBUTING.md)
- Review architecture first
- Follow branch naming conventions
- Write tests for new features
- Update documentation

## Support

For questions:
- Open a GitHub issue
- Check existing documentation
- Review code comments

