/**
 * Forget operation - Remove memory entries
 */

import type { ForgetResult } from "../types/memory.js";
import { findEntryById, removeEntry } from "../storage/file-manager.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
} from "../storage/scope-resolver.js";

/**
 * Forget (delete) a memory entry by ID
 */
export async function forgetMemory(entryId: string): Promise<ForgetResult> {
  // Search in project scope first
  let found = await findEntryById(resolveProjectRoot(), entryId);

  // If not found, search in global scope
  if (!found) {
    found = await findEntryById(resolveGlobalRoot(), entryId);
  }

  if (!found) {
    return {
      forgotten: false,
      entry_id: entryId,
      was_in: "",
    };
  }

  // Remove entry
  const success = await removeEntry(found.filePath, entryId);

  return {
    forgotten: success,
    entry_id: entryId,
    was_in: found.filePath,
  };
}
