# Changelog

All notable changes to MemoryKit.

**Format:** [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | **Versioning:** [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

---

## [Unreleased]

### ✨ Added

**Documentation:**

- Scientific overview explaining the LLM memory problem ([SCIENTIFIC_OVERVIEW.md](docs/SCIENTIFIC_OVERVIEW.md))
- Development guide with best practices ([DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md))
- XML documentation for all public Domain interfaces
- GDPR compliance documentation

**Features:**

- `CreateConversationCommand` and handler for conversation creation
- `DeleteUserDataAsync` for GDPR compliance in MemoryOrchestrator
- `AnswerWithContextAsync` in ISemanticKernelService interface
- Backward compatibility wrappers for deprecated interfaces

**Infrastructure:**

- `/SECRETS` folder for confidential documentation (gitignored)

### 🔄 Changed

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

### ⚠️ Deprecated

All interface definitions in Infrastructure layer (use `Domain.Interfaces` instead):

- `Infrastructure.Azure.AzureServiceInterfaces`
- `Infrastructure.Cognitive.CognitiveInterfaces`
- `Infrastructure.SemanticKernel.SemanticKernelInterfaces`

### 🗑️ Removed

**Architecture:**

- Circular dependency between Application and Infrastructure layers

**Documentation:**

- Redundant completion docs (PROJECT_COMPLETE.md, IMPLEMENTATION_COMPLETE.md, etc.)
- `docs/archive/WORKFLOW_REVISION_SUMMARY.md` (content in CHANGELOG.md)
- Excessive code examples from README.md (available in `/SECRETS/README_FULL_TRD.md`)

### 🐛 Fixed

**Critical:**

- Constructor parameter mismatch in [Program.cs](src/MemoryKit.API/Program.cs) (MemoryOrchestrator)
- Circular dependency between Application and Infrastructure layers
- Missing `DeleteUserDataAsync` for GDPR compliance

**Architecture:**

- Infrastructure layer defining interfaces instead of Domain
- Mismatched `ISemanticKernelService` interface/implementation signatures
- Duplicate `ConsolidationMetrics` definition

**Code Quality:**

- Benchmark constructor issues in [MemoryRetrievalBenchmarks.cs](tests/MemoryKit.Benchmarks/MemoryRetrievalBenchmarks.cs)
- ExtractedFact using object initializer instead of factory method
- DebugInfo type inconsistency (QueryPlanType string vs QueryType enum)
- CreateConversation endpoint NotImplementedException

---

## [1.0.0] - 2025-11-16

### ✨ Initial Release

**Core:**

- Clean Architecture with 4-layer design
- Neuroscience-inspired memory system
- CQRS with MediatR
- In-memory implementations

**Documentation:**

- Architecture guide
- API reference
- Deployment guide
- Contributing guidelines

[Unreleased]: https://github.com/antoniorapozo/memorykit/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/antoniorapozo/memorykit/releases/tag/v1.0.0
