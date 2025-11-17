# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive scientific documentation (`docs/SCIENTIFIC_OVERVIEW.md`) explaining how MemoryKit solves the LLM memory problem
- Development guide (`DEVELOPMENT_GUIDE.md`) for contributors with best practices and workflows
- Complete XML documentation for all public interfaces in Domain layer
- `AnswerWithContextAsync` method to ISemanticKernelService interface
- Backward compatibility wrappers for deprecated interfaces with migration guidance

### Changed
- **BREAKING**: Moved all service interfaces from Infrastructure to Domain layer for proper Clean Architecture
  - `IWorkingMemoryService`, `IScratchpadService`, `IEpisodicMemoryService`, `IProceduralMemoryService` → `MemoryKit.Domain.Interfaces`
  - `IAmygdalaImportanceEngine`, `IHippocampusIndexer`, `IPrefrontalController` → `MemoryKit.Domain.Interfaces`
  - `ISemanticKernelService` → `MemoryKit.Domain.Interfaces`
- Updated all using statements across codebase to reference `MemoryKit.Domain.Interfaces`
- Aligned `ISemanticKernelService` interface methods with implementation (renamed `GenerateEmbeddingsAsync` → `GetEmbeddingAsync`, added `ClassifyQueryAsync`, `ExtractEntitiesAsync`)

### Deprecated
- Interface definitions in `MemoryKit.Infrastructure.Azure.AzureServiceInterfaces` (use `MemoryKit.Domain.Interfaces` instead)
- Interface definitions in `MemoryKit.Infrastructure.Cognitive.CognitiveInterfaces` (use `MemoryKit.Domain.Interfaces` instead)
- Interface definitions in `MemoryKit.Infrastructure.SemanticKernel.SemanticKernelInterfaces` (use `MemoryKit.Domain.Interfaces` instead)

### Removed
- Circular dependency between Application and Infrastructure layers

### Fixed
- **CRITICAL**: Circular dependency issue where Application referenced Infrastructure AND Infrastructure referenced Application
- Architecture violation where Infrastructure layer defined interfaces instead of Domain layer
- Mismatched interface method signatures between `ISemanticKernelService` definition and `SemanticKernelService` implementation
- Duplicate `ConsolidationMetrics` definition

### Security
- No security changes in this release

## [1.0.0] - 2025-11-16

### Added
- Initial release
- Project scaffolding complete
- Architecture documentation
- API documentation
- Deployment guide
- Contributing guidelines

[Unreleased]: https://github.com/antoniorapozo/memorykit/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/antoniorapozo/memorykit/releases/tag/v1.0.0
