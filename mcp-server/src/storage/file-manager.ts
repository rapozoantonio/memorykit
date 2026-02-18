/**
 * File manager - Read/write operations for memory files
 */

import { readFile, writeFile, mkdir, readdir, stat, unlink } from "fs/promises";
import { existsSync } from "fs";
import { join, dirname, basename } from "path";
import type { MemoryEntry, FileInfo } from "../types/memory.js";
import { MemoryLayer } from "../types/memory.js";
import {
  parseEntries,
  serializeEntries,
  extractHeader,
} from "./entry-parser.js";

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
    return parseEntries(content);
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
 * Append a single entry to file
 */
export async function appendEntry(
  filePath: string,
  entry: MemoryEntry,
): Promise<void> {
  // Read existing entries
  const existingEntries = await readMemoryFile(filePath);

  // Extract header if file exists
  let header: string | undefined;
  if (existsSync(filePath)) {
    const content = await readFile(filePath, "utf-8");
    header = extractHeader(content);
  }

  // Add new entry
  existingEntries.push(entry);

  // Write back
  await writeMemoryFile(filePath, existingEntries, header);
}

/**
 * Remove entry by ID from file
 */
export async function removeEntry(
  filePath: string,
  entryId: string,
): Promise<boolean> {
  if (!existsSync(filePath)) {
    return false;
  }

  // Read existing entries
  const entries = await readMemoryFile(filePath);

  // Filter out the entry
  const filteredEntries = entries.filter((e) => e.id !== entryId);

  // Check if anything was removed
  if (filteredEntries.length === entries.length) {
    return false; // Entry not found
  }

  // If no entries left, delete the file
  if (filteredEntries.length === 0) {
    await unlink(filePath);
    return true;
  }

  // Extract header
  const content = await readFile(filePath, "utf-8");
  const header = extractHeader(content);

  // Write back remaining entries
  await writeMemoryFile(filePath, filteredEntries, header);
  return true;
}

/**
 * Update entry by ID in file
 */
export async function updateEntry(
  filePath: string,
  entryId: string,
  updates: Partial<MemoryEntry>,
): Promise<boolean> {
  if (!existsSync(filePath)) {
    return false;
  }

  // Read existing entries
  const entries = await readMemoryFile(filePath);

  // Find and update entry
  let found = false;
  const updatedEntries = entries.map((entry) => {
    if (entry.id === entryId) {
      found = true;
      return {
        ...entry,
        ...updates,
        updated: new Date().toISOString(),
      };
    }
    return entry;
  });

  if (!found) {
    return false;
  }

  // Extract header
  const content = await readFile(filePath, "utf-8");
  const header = extractHeader(content);

  // Write back
  await writeMemoryFile(filePath, updatedEntries, header);
  return true;
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
