/**
 * Entry parser - Parse and serialize memory entries with blockquote metadata
 */

import { createHash } from "crypto";
import type {
  MemoryEntry,
  EntryMetadata,
  MemorySource,
} from "../types/memory.js";

/**
 * Parse a blockquote metadata line
 * Format: > importance: 0.85 | created: 2026-02-16T10:30:00Z | tags: database, architecture | source: conversation
 */
export function parseMetadataLine(line: string): EntryMetadata {
  // Remove leading '>' and trim
  const cleaned = line.replace(/^>\s*/, "").trim();

  // Split by '|' separator
  const pairs = cleaned.split("|").map((p) => p.trim());

  const metadata: Partial<EntryMetadata> = {};

  for (const pair of pairs) {
    // Split on first colon only to handle values containing colons (e.g., timestamps)
    const [key, ...valueParts] = pair.split(":");
    if (!key || valueParts.length === 0) continue;

    const keyTrimmed = key.trim();
    const value = valueParts.join(":").trim();

    switch (keyTrimmed) {
      case "importance":
        // parseFloat with fallback to prevent NaN propagation
        metadata.importance = parseFloat(value) || 0.5;
        break;
      case "created":
        metadata.created = value;
        break;
      case "updated":
        metadata.updated = value;
        break;
      case "tags":
        metadata.tags = value
          .split(",")
          .map((t) => t.trim())
          .filter((t) => t);
        break;
      case "source":
        metadata.source = value as MemorySource;
        break;
      case "decay_rate":
        metadata.decay_rate = parseFloat(value) || undefined;
        break;
      case "promoted_from":
        metadata.promoted_from = value;
        break;
    }
  }

  // Set defaults for required fields
  return {
    importance: metadata.importance ?? 0.5,
    created: metadata.created ?? new Date().toISOString(),
    tags: metadata.tags ?? [],
    source: metadata.source ?? "conversation",
    updated: metadata.updated,
    decay_rate: metadata.decay_rate,
    promoted_from: metadata.promoted_from,
  };
}

/**
 * Generate entry ID: e_{timestamp}_{4_char_hash}
 */
export function generateEntryId(content: string): string {
  const timestamp = Math.floor(Date.now() / 1000);
  const hash = createHash("sha256")
    .update(content)
    .digest("hex")
    .substring(0, 4);
  return `e_${timestamp}_${hash}`;
}

/**
 * Parse a single entry from raw text
 * Format:
 * > importance: 0.85 | created: ... | tags: ... | source: ...
 *
 * Entry content here...
 */
export function parseEntry(rawText: string): MemoryEntry | null {
  const lines = rawText.split("\n");

  // Find metadata line (starts with '>')
  let metadataLine: string | null = null;
  let contentStartIndex = 0;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    if (line.startsWith(">") && line.includes("importance:")) {
      metadataLine = line;
      contentStartIndex = i + 1;
      break;
    }
  }

  if (!metadataLine) return null;

  // Parse metadata
  const metadata = parseMetadataLine(metadataLine);

  // Extract content (everything after metadata line)
  const content = lines.slice(contentStartIndex).join("\n").trim();

  if (!content) return null;

  // Generate ID from content
  const id = generateEntryId(content);

  return {
    id,
    content,
    importance: metadata.importance,
    created: metadata.created,
    tags: metadata.tags,
    source: metadata.source,
    updated: metadata.updated,
    decay_rate: metadata.decay_rate,
    promoted_from: metadata.promoted_from as any,
  };
}

/**
 * Serialize entry to markdown format
 */
export function serializeEntry(entry: MemoryEntry): string {
  const parts: string[] = [];

  // Build metadata line
  const metadataParts = [
    `importance: ${entry.importance.toFixed(2)}`,
    `created: ${entry.created}`,
    `tags: ${entry.tags.join(", ")}`,
    `source: ${entry.source}`,
  ];

  if (entry.updated) {
    metadataParts.push(`updated: ${entry.updated}`);
  }
  if (entry.decay_rate !== undefined) {
    metadataParts.push(`decay_rate: ${entry.decay_rate}`);
  }
  if (entry.promoted_from) {
    metadataParts.push(`promoted_from: ${entry.promoted_from}`);
  }

  parts.push(`> ${metadataParts.join(" | ")}`);
  parts.push("");
  parts.push(entry.content);

  return parts.join("\n");
}

/**
 * Parse multiple entries from a file content
 * Entries are separated by '---'
 */
export function parseEntries(fileContent: string): MemoryEntry[] {
  const entries: MemoryEntry[] = [];

  // Split by horizontal rule (---)
  const sections = fileContent.split(/\n---\n/);

  for (const section of sections) {
    const trimmed = section.trim();
    if (!trimmed || trimmed.startsWith("#")) {
      // Skip empty sections or header-only sections
      continue;
    }

    const entry = parseEntry(trimmed);
    if (entry) {
      entries.push(entry);
    }
  }

  return entries;
}

/**
 * Serialize multiple entries to file content
 */
export function serializeEntries(
  entries: MemoryEntry[],
  header?: string,
): string {
  const parts: string[] = [];

  if (header) {
    parts.push(header);
    parts.push("");
  }

  for (let i = 0; i < entries.length; i++) {
    if (i > 0) {
      parts.push("---");
      parts.push("");
    }
    parts.push(serializeEntry(entries[i]));
  }

  return parts.join("\n");
}

/**
 * Extract header from file content (everything before first entry)
 */
export function extractHeader(fileContent: string): string {
  const lines = fileContent.split("\n");
  const headerLines: string[] = [];

  for (const line of lines) {
    if (line.trim().startsWith(">") && line.includes("importance:")) {
      break;
    }
    headerLines.push(line);
  }

  return headerLines.join("\n").trim();
}
