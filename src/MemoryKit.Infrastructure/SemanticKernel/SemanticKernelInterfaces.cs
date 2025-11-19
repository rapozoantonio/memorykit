// ============================================================================
// OBSOLETE: These interfaces have been moved to MemoryKit.Domain.Interfaces
// ============================================================================
// This file is kept for backward compatibility but will be removed in v2.0
// All interfaces are now defined in MemoryKit.Domain.Interfaces.DomainInterfaces
//
// Migration: Replace all using MemoryKit.Infrastructure.SemanticKernel;
//            with using MemoryKit.Domain.Interfaces;
// ============================================================================

using MemoryKit.Domain.Interfaces;

namespace MemoryKit.Infrastructure.SemanticKernel;

/// <summary>
/// OBSOLETE: Use MemoryKit.Domain.Interfaces.ISemanticKernelService instead.
/// </summary>
[Obsolete("This interface has been moved to MemoryKit.Domain.Interfaces. Use ISemanticKernelService from MemoryKit.Domain.Interfaces instead.", error: false)]
public interface ISemanticKernelService : Domain.Interfaces.ISemanticKernelService { }
