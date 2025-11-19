// ============================================================================
// OBSOLETE: These interfaces have been moved to MemoryKit.Domain.Interfaces
// ============================================================================
// This file is kept for backward compatibility but will be removed in v2.0
// All interfaces are now defined in MemoryKit.Domain.Interfaces.DomainInterfaces
//
// Migration: Replace all using MemoryKit.Infrastructure.Cognitive;
//            with using MemoryKit.Domain.Interfaces;
// ============================================================================

using MemoryKit.Domain.Interfaces;

namespace MemoryKit.Infrastructure.Cognitive;

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.IAmygdalaImportanceEngine instead.
/// </summary>
[Obsolete("This interface has been moved to MemoryKit.Domain.Interfaces. Use IAmygdalaImportanceEngine from MemoryKit.Domain.Interfaces instead.", error: false)]
public interface IAmygdalaImportanceEngine : Domain.Interfaces.IAmygdalaImportanceEngine { }

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.IHippocampusIndexer instead.
/// </summary>
[Obsolete("This interface has been moved to MemoryKit.Domain.Interfaces. Use IHippocampusIndexer from MemoryKit.Domain.Interfaces instead.", error: false)]
public interface IHippocampusIndexer : Domain.Interfaces.IHippocampusIndexer { }

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.IPrefrontalController instead.
/// </summary>
[Obsolete("This interface has been moved to MemoryKit.Domain.Interfaces. Use IPrefrontalController from MemoryKit.Domain.Interfaces instead.", error: false)]
public interface IPrefrontalController : Domain.Interfaces.IPrefrontalController { }

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.ConsolidationMetrics instead.
/// </summary>
[Obsolete("This type has been moved to MemoryKit.Domain.Interfaces. Use ConsolidationMetrics from MemoryKit.Domain.Interfaces instead.", error: false)]
public record ConsolidationMetrics : Domain.Interfaces.ConsolidationMetrics { }
