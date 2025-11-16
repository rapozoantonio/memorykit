# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-16

### Added
- Initial release of MemoryKit
- MIT License
- Comprehensive README with project overview
- Contributing guidelines
- Documentation:
  - Architecture overview (Clean Architecture with cognitive memory model)
  - API reference documentation
  - Deployment guide (Azure, Docker, Kubernetes)
  - Cognitive model explanation (inspired by neuroscience)
- Project structure and solution file
- GitHub Actions workflows for CI/CD
- .gitignore for .NET projects

### Project Structure
- Solution organized following Clean Architecture:
  - Domain Layer (entities, value objects, interfaces)
  - Application Layer (use cases, services)
  - Infrastructure Layer (Azure services, in-memory implementations)
  - API Layer (REST controllers)

### Memory Systems
- Working Memory (Redis-based short-term storage)
- Episodic Memory (Long-term conversation storage)
- Semantic Memory (Fact and knowledge storage)
- Procedural Memory (Learned patterns and behaviors)

### Cognitive Components
- Amygdala Importance Engine (emotional significance assessment)
- Hippocampus Memory Indexer (memory consolidation)
- Prefrontal Controller (query planning and execution)

### Documentation
- Full API documentation with examples
- Architecture documentation with layer descriptions
- Deployment guides for multiple platforms
- Cognitive model explanation based on neuroscience research

[1.0.0]: https://github.com/rapozoantonio/memorykit/releases/tag/v1.0.0
