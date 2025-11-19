// ============================================================================
// OBSOLETE: These interfaces have been moved to MemoryKit.Domain.Interfaces
// ============================================================================
// This file is kept for backward compatibility but will be removed in v2.0
// All interfaces are now defined in MemoryKit.Domain.Interfaces.DomainInterfaces
//
// Migration: Replace all using MemoryKit.Infrastructure.Azure;
//            with using MemoryKit.Domain.Interfaces;
// ============================================================================

using MemoryKit.Domain.Interfaces;

namespace MemoryKit.Infrastructure.Azure;

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.IWorkingMemoryService instead.
/// </summary>
[Obsolete("This interface has been moved to MemoryKit.Domain.Interfaces. Use IWorkingMemoryService from MemoryKit.Domain.Interfaces instead.", error: false)]
public interface IWorkingMemoryService : Domain.Interfaces.IWorkingMemoryService { }

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.IScratchpadService instead.
/// </summary>
[Obsolete("This interface has been moved to MemoryKit.Domain.Interfaces. Use IScratchpadService from MemoryKit.Domain.Interfaces instead.", error: false)]
public interface IScratchpadService : Domain.Interfaces.IScratchpadService { }

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.IEpisodicMemoryService instead.
/// </summary>
[Obsolete("This interface has been moved to MemoryKit.Domain.Interfaces. Use IEpisodicMemoryService from MemoryKit.Domain.Interfaces instead.", error: false)]
public interface IEpisodicMemoryService : Domain.Interfaces.IEpisodicMemoryService { }

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.IProceduralMemoryService instead.
/// </summary>
[Obsolete("This interface has been moved to MemoryKit.Domain.Interfaces. Use IProceduralMemoryService from MemoryKit.Domain.Interfaces instead.", error: false)]
public interface IProceduralMemoryService : Domain.Interfaces.IProceduralMemoryService { }
