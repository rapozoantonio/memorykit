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
- `/SECRETS` folder with README.md documenting confidential documentation strategy

### Changed
- **BREAKING**: Moved all service interfaces from Infrastructure to Domain layer for proper Clean Architecture
  - `IWorkingMemoryService`, `IScratchpadService`, `IEpisodicMemoryService`, `IProceduralMemoryService` → `MemoryKit.Domain.Interfaces`
  - `IAmygdalaImportanceEngine`, `IHippocampusIndexer`, `IPrefrontalController` → `MemoryKit.Domain.Interfaces`
  - `ISemanticKernelService` → `MemoryKit.Domain.Interfaces`
- Updated all using statements across codebase to reference `MemoryKit.Domain.Interfaces`
- Aligned `ISemanticKernelService` interface methods with implementation (renamed `GenerateEmbeddingsAsync` → `GetEmbeddingAsync`, added `ClassifyQueryAsync`, `ExtractEntitiesAsync`)
- Updated FluentValidation.AspNetCore package from 11.3.0 to 11.8.0 for consistency
- Improved GetStatistics endpoint with clear documentation about limitations
- **README.md optimized from 2,800 to 400 lines (86% reduction)** - Now focused on selling the project with goldfish problem, neuroscience solution, and clear value proposition
- **docs/README.md simplified** - Removed duplicate content, now serves as clean navigation index
- Moved complete technical requirements document to `/SECRETS/README_FULL_TRD.md`
- Moved `IMPLEMENTATION_SUMMARY.md` and `FILE_STRUCTURE.md` to `/SECRETS` folder

### Deprecated
- Interface definitions in `MemoryKit.Infrastructure.Azure.AzureServiceInterfaces` (use `MemoryKit.Domain.Interfaces` instead)
- Interface definitions in `MemoryKit.Infrastructure.Cognitive.CognitiveInterfaces` (use `MemoryKit.Domain.Interfaces` instead)
- Interface definitions in `MemoryKit.Infrastructure.SemanticKernel.SemanticKernelInterfaces` (use `MemoryKit.Domain.Interfaces` instead)

### Removed
- Circular dependency between Application and Infrastructure layers
- Redundant completion documentation files (PROJECT_COMPLETE.md, IMPLEMENTATION_COMPLETE.md, COMPLETION_CHECKLIST.md)
- `docs/archive/WORKFLOW_REVISION_SUMMARY.md` - Content already documented in CHANGELOG.md
- Excessive code examples from README.md - Available in repository and `/SECRETS/README_FULL_TRD.md`

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
- Reorganized documentation structure with clear separation between public and confidential content
- Created `/SECRETS` folder for proprietary documentation (excluded from git)
- Optimized README.md for engagement while maintaining technical accuracy
- Simplified docs/README.md to serve as navigation index
- Removed redundant documentation files (LAYERED_MEMORY_REVIEW.md, PRODUCTION_READY.md, FIXES_APPLIED.md, WORKFLOW_REVISION_SUMMARY.md)
- Streamlined PROJECT_STATUS.md to remove duplicate content from CHANGELOG.md
- Consolidated all historical changes into CHANGELOG.md

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
