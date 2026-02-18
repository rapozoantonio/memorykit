/**
 * Update operation - Modify existing memory entries
 */

import type {
  UpdateResult,
  UpdateOptions,
  MemoryEntry,
} from "../types/memory.js";
import { findEntryById, updateEntry } from "../storage/file-manager.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
} from "../storage/scope-resolver.js";
import { calculateImportance } from "../cognitive/amygdala.js";

/**
 * Update a memory entry by ID
 */
export async function updateMemory(
  entryId: string,
  updates: UpdateOptions,
): Promise<UpdateResult> {
  // Search in project scope first
  let found = await findEntryById(resolveProjectRoot(), entryId);

  // If not found, search in global scope
  if (!found) {
    found = await findEntryById(resolveGlobalRoot(), entryId);
  }

  if (!found) {
    return {
      updated: false,
      entry_id: entryId,
      file: "",
    };
  }

  // Build update object
  const entryUpdates: Partial<MemoryEntry> = {};

  if (updates.content !== undefined) {
    entryUpdates.content = updates.content;

    // Re-calculate importance if content changed (unless manually overridden)
    if (updates.importance === undefined) {
      entryUpdates.importance = calculateImportance(updates.content);
    }
  }

  if (updates.tags !== undefined) {
    entryUpdates.tags = updates.tags;
  }

  if (updates.importance !== undefined) {
    entryUpdates.importance = updates.importance;
  }

  // Apply update
  const success = await updateEntry(found.filePath, entryId, entryUpdates);

  return {
    updated: success,
    entry_id: entryId,
    file: found.filePath,
    new_importance: entryUpdates.importance,
  };
}
