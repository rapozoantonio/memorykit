/**
 * File manager - Read/write operations for memory files
 */

import { readFile, writeFile, mkdir, readdir, stat, unlink } from "fs/promises";
import { existsSync } from "fs";
import { join, dirname, basename } from "path";

// ─── Per-file write lock ────────────────────────────────────────────────────────
// Serializes concurrent write operations on the same file path.
// JavaScript is single-threaded so the get→set sequence has no race condition.

const _writeLocks = new Map<string, Promise<void>>();

async function acquireFileLock(filePath: string): Promise<() => void> {
  const prev = _writeLocks.get(filePath);
  let release!: () => void;
  const current = new Promise<void>((res) => {
    release = res;
  });
  _writeLocks.set(filePath, current);
  if (prev) await prev;
  return () => {
    release();
    if (_writeLocks.get(filePath) === current) {
      _writeLocks.delete(filePath);
    }
  };
}

async function withFileLock<T>(
  filePath: string,
  fn: () => Promise<T>,
): Promise<T> {
  const release = await acquireFileLock(filePath);
  try {
    return await fn();
  } finally {
    release();
  }
}
import type { MemoryEntry, FileInfo } from "../types/memory.js";
import { MemoryLayer } from "../types/memory.js";
import {
  parseEntries,
  serializeEntries,
  extractHeader,
} from "./entry-parser.js";

/**
 * Infer memory layer from file path
 */
function getLayerFromPath(filePath: string): MemoryLayer {
  const normalized = filePath.replace(/\\/g, "/").toLowerCase();

  if (normalized.includes("/working/")) return MemoryLayer.Working;
  if (normalized.includes("/facts/")) return MemoryLayer.Facts;
  if (normalized.includes("/episodes/")) return MemoryLayer.Episodes;
  if (normalized.includes("/procedures/")) return MemoryLayer.Procedures;

  // Default to working if we can't determine
  return MemoryLayer.Working;
}

/**
 * Infer scope from file path
 * Global: ~/.memorykit/facts/...
 * Project: ~/.memorykit/<projectName>/facts/...
 */
function getScopeFromPath(filePath: string): "project" | "global" {
  const normalized = filePath.replace(/\\/g, "/");

  // Find .memorykit in path
  const memoryKitIndex = normalized.indexOf("/.memorykit/");
  if (memoryKitIndex === -1) {
    return "project"; // Not in .memorykit directory, assume project
  }

  // Extract the part after /.memorykit/
  const afterMemoryKit = normalized.substring(
    memoryKitIndex + "/.memorykit/".length,
  );

  // Split by / to get path segments
  const segments = afterMemoryKit.split("/").filter((s) => s);

  if (segments.length === 0) {
    return "project"; // No segments, shouldn't happen
  }

  // Check if first segment is a layer name (indicates global scope)
  const firstSegment = segments[0].toLowerCase();
  const layerNames = ["working", "facts", "episodes", "procedures"];

  if (layerNames.includes(firstSegment)) {
    // Path is ~/.memorykit/<layer>/... → global scope
    return "global";
  }

  // Path is ~/.memorykit/<projectName>/<layer>/... → project scope
  return "project";
}

/**
 * Ensure directory exists, create if not
 */
export async function ensureDirectoryExists(dirPath: string): Promise<void> {
  if (!existsSync(dirPath)) {
    await mkdir(dirPath, { recursive: true });
  }
}

/**
 * Read memory file and parse entries
 */
export async function readMemoryFile(filePath: string): Promise<MemoryEntry[]> {
  if (!existsSync(filePath)) {
    return [];
  }

  try {
    const content = await readFile(filePath, "utf-8");
    const layer = getLayerFromPath(filePath);
    const scope = getScopeFromPath(filePath);
    return parseEntries(content, layer, scope, filePath);
  } catch (error) {
    console.error(`Failed to read memory file ${filePath}:`, error);
    return [];
  }
}

/**
 * Write entries to memory file
 */
export async function writeMemoryFile(
  filePath: string,
  entries: MemoryEntry[],
  header?: string,
): Promise<void> {
  try {
    // Ensure directory exists
    await ensureDirectoryExists(dirname(filePath));

    // Serialize and write
    const content = serializeEntries(entries, header);
    await writeFile(filePath, content, "utf-8");
  } catch (error) {
    console.error(`Failed to write memory file ${filePath}:`, error);
    throw error;
  }
}

/**
 * Append a single entry to file (serialized per file path)
 */
export async function appendEntry(
  filePath: string,
  entry: MemoryEntry,
): Promise<void> {
  await withFileLock(filePath, async () => {
    const existingEntries = await readMemoryFile(filePath);
    let header: string | undefined;
    if (existsSync(filePath)) {
      const content = await readFile(filePath, "utf-8");
      header = extractHeader(content);
    }
    existingEntries.push(entry);
    await writeMemoryFile(filePath, existingEntries, header);
  });
}

/**
 * Remove entry by ID from file (serialized per file path)
 */
export async function removeEntry(
  filePath: string,
  entryId: string,
): Promise<boolean> {
  return withFileLock(filePath, async () => {
    if (!existsSync(filePath)) {
      return false;
    }
    const entries = await readMemoryFile(filePath);
    const filteredEntries = entries.filter((e) => e.id !== entryId);
    if (filteredEntries.length === entries.length) {
      return false;
    }
    if (filteredEntries.length === 0) {
      await unlink(filePath);
      return true;
    }
    const content = await readFile(filePath, "utf-8");
    const header = extractHeader(content);
    await writeMemoryFile(filePath, filteredEntries, header);
    return true;
  });
}

/**
 * Update entry by ID in file (serialized per file path)
 */
export async function updateEntry(
  filePath: string,
  entryId: string,
  updates: Partial<MemoryEntry>,
): Promise<boolean> {
  return withFileLock(filePath, async () => {
    if (!existsSync(filePath)) {
      return false;
    }
    const entries = await readMemoryFile(filePath);
    let found = false;
    const updatedEntries = entries.map((entry) => {
      if (entry.id === entryId) {
        found = true;
        return { ...entry, ...updates, updated: new Date().toISOString() };
      }
      return entry;
    });
    if (!found) {
      return false;
    }
    const content = await readFile(filePath, "utf-8");
    const header = extractHeader(content);
    await writeMemoryFile(filePath, updatedEntries, header);
    return true;
  });
}

/**
 * List all memory files in a directory
 */
export async function listMemoryFiles(rootPath: string): Promise<FileInfo[]> {
  if (!existsSync(rootPath)) {
    return [];
  }

  const files: FileInfo[] = [];

  // Check each layer directory
  const layers: MemoryLayer[] = [
    MemoryLayer.Working,
    MemoryLayer.Facts,
    MemoryLayer.Episodes,
    MemoryLayer.Procedures,
  ];

  for (const layer of layers) {
    const layerPath = join(rootPath, layer);
    if (!existsSync(layerPath)) continue;

    try {
      const fileNames = await readdir(layerPath);

      for (const fileName of fileNames) {
        if (!fileName.endsWith(".md")) continue;

        const filePath = join(layerPath, fileName);
        const stats = await stat(filePath);

        // Count entries
        const entries = await readMemoryFile(filePath);

        files.push({
          path: filePath,
          filename: fileName,
          layer,
          entryCount: entries.length,
          size: stats.size,
        });
      }
    } catch (error) {
      console.error(`Failed to list files in ${layerPath}:`, error);
    }
  }

  return files;
}

/**
 * Find entry by ID across all files in a root
 */
export async function findEntryById(
  rootPath: string,
  entryId: string,
): Promise<{ entry: MemoryEntry; filePath: string } | null> {
  const files = await listMemoryFiles(rootPath);

  for (const fileInfo of files) {
    const entries = await readMemoryFile(fileInfo.path);
    const entry = entries.find((e) => e.id === entryId);

    if (entry) {
      return { entry, filePath: fileInfo.path };
    }
  }

  return null;
}

/**
 * Get total entry count in a root
 */
export async function getTotalEntryCount(rootPath: string): Promise<number> {
  const files = await listMemoryFiles(rootPath);
  return files.reduce((sum, file) => sum + file.entryCount, 0);
}

/**
 * Check if file exists and is readable
 */
export function isFileAccessible(filePath: string): boolean {
  return existsSync(filePath);
}
