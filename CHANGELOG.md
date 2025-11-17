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
- `CreateConversationCommand` and handler for conversation creation endpoint
- `DeleteUserDataAsync` implementation in MemoryOrchestrator for GDPR compliance

### Changed
- **BREAKING**: Moved all service interfaces from Infrastructure to Domain layer for proper Clean Architecture
  - `IWorkingMemoryService`, `IScratchpadService`, `IEpisodicMemoryService`, `IProceduralMemoryService` → `MemoryKit.Domain.Interfaces`
  - `IAmygdalaImportanceEngine`, `IHippocampusIndexer`, `IPrefrontalController` → `MemoryKit.Domain.Interfaces`
  - `ISemanticKernelService` → `MemoryKit.Domain.Interfaces`
- Updated all using statements across codebase to reference `MemoryKit.Domain.Interfaces`
- Aligned `ISemanticKernelService` interface methods with implementation (renamed `GenerateEmbeddingsAsync` → `GetEmbeddingAsync`, added `ClassifyQueryAsync`, `ExtractEntitiesAsync`)
- Updated FluentValidation.AspNetCore package from 11.3.0 to 11.8.0 for consistency
- Improved GetStatistics endpoint with clear documentation about MVP limitations

### Deprecated
- Interface definitions in `MemoryKit.Infrastructure.Azure.AzureServiceInterfaces` (use `MemoryKit.Domain.Interfaces` instead)
- Interface definitions in `MemoryKit.Infrastructure.Cognitive.CognitiveInterfaces` (use `MemoryKit.Domain.Interfaces` instead)
- Interface definitions in `MemoryKit.Infrastructure.SemanticKernel.SemanticKernelInterfaces` (use `MemoryKit.Domain.Interfaces` instead)

### Removed
- Circular dependency between Application and Infrastructure layers
- Redundant completion documentation files (PROJECT_COMPLETE.md, IMPLEMENTATION_COMPLETE.md, COMPLETION_CHECKLIST.md)

### Fixed
- **CRITICAL**: Constructor parameter mismatch in Program.cs (MemoryOrchestrator instantiation with incorrect parameter count)
- **CRITICAL**: Circular dependency issue where Application referenced Infrastructure AND Infrastructure referenced Application
- **CRITICAL**: Missing DeleteUserDataAsync implementation for GDPR compliance
- Benchmark code constructor issues in MemoryRetrievalBenchmarks.cs (PrefrontalController and AmygdalaImportanceEngine)
- Benchmark code ExtractedFact initialization using object initializer instead of factory method
- DebugInfo type inconsistency in QueryMemoryHandler (QueryPlanType string vs QueryType enum)
- CreateConversation endpoint NotImplementedException
- Architecture violation where Infrastructure layer defined interfaces instead of Domain layer
- Mismatched interface method signatures between `ISemanticKernelService` definition and `SemanticKernelService` implementation
- Duplicate `ConsolidationMetrics` definition

### Security
- No security changes in this release

### Documentation
- Archived WORKFLOW_REVISION_SUMMARY.md to docs/archive/
- Consolidated and cleaned up redundant documentation files

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
