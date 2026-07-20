/**
 * Forget operation - Remove memory entries
 */

import { resolve, sep } from "path";
import type { ForgetResult } from "../types/memory.js";
import { findEntryById, removeEntry } from "../storage/file-manager.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
} from "../storage/scope-resolver.js";
import { removeEntryFromGraph } from "./entity-graph.js";

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

  // Determine scope from path using resolved absolute paths to handle
  // Windows path separators and case-sensitivity differences.
  // The trailing sep prevents prefix collisions between project names
  // like "my-app" and "my-app-v2" sharing the same root prefix.
  const normalizedFilePath = resolve(found.filePath);
  const normalizedProjectRoot = resolve(resolveProjectRoot()) + sep;
  const scope = normalizedFilePath.startsWith(normalizedProjectRoot)
    ? "project"
    : "global";

  // Remove from entity graph first (prevents dangling references)
  await removeEntryFromGraph(entryId, scope);

  // Remove entry from file
  const success = await removeEntry(found.filePath, entryId);

  return {
    forgotten: success,
    entry_id: entryId,
    was_in: found.filePath,
  };
}
