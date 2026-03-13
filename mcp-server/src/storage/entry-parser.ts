/**
 * Entry parser - Parse and serialize memory entries in MML (Markdown Memory Language) format
 *
 * MML Format:
 * ### Heading
 * - **key**: value
 * - **key2**: value2
 */

import { createHash } from "crypto";
import type {
  MemoryEntry,
  MemoryLayer,
  AcquisitionContext,
} from "../types/memory.js";

/**
 * Generate entry ID from heading and created timestamp
 * Format: e_{timestamp}_{4_char_hash_of_heading}
 */
export function generateEntryId(heading: string, created: string): string {
  // Use created date timestamp if available, otherwise current time
  let timestamp: number;
  try {
    timestamp = Math.floor(new Date(created).getTime() / 1000);
  } catch {
    timestamp = Math.floor(Date.now() / 1000);
  }

  const hash = createHash("sha256")
    .update(heading)
    .digest("hex")
    .substring(0, 4);
  return `e_${timestamp}_${hash}`;
}

/**
 * Parse acquisition context from compact format: "8240t, 5tc"
 */
export function parseAcquisition(value: string): AcquisitionContext | null {
  const match = value.match(/(\d+)t,\s*(\d+)tc/);
  if (!match) return null;
  return {
    tokens_consumed: parseInt(match[1], 10),
    tool_calls: parseInt(match[2], 10),
  };
}

/**
 * Serialize acquisition context to compact format: "8240t, 5tc"
 */
export function serializeAcquisition(ctx: AcquisitionContext): string {
  return `${ctx.tokens_consumed}t, ${ctx.tool_calls}tc`;
}

/**
 * Parse a single MML key-value line
 * Format: - **key**: value
 * Handles colons in values correctly by splitting only on first ": " after **key**
 */
export function parseMMLLine(line: string): [string, string] | null {
  const trimmed = line.trim();

  // Must start with "- **" to be a valid MML line
  if (!trimmed.startsWith("- **")) {
    return null;
  }

  // Extract key between ** markers
  const keyEndIndex = trimmed.indexOf("**:", 4);
  if (keyEndIndex === -1) {
    return null;
  }

  const key = trimmed.substring(4, keyEndIndex).trim();

  // Value is everything after "**: "
  const valueStart = keyEndIndex + 3;
  const value = trimmed.substring(valueStart).trim();

  if (!key) {
    return null;
  }

  return [key, value];
}

/**
 * Parse a single MML entry from raw text
 * Entry must start with ### heading followed by - **key**: value lines
 */
export function parseEntry(
  rawText: string,
  layer: MemoryLayer,
  scope: "project" | "global",
  filePath: string,
): MemoryEntry | null {
  const lines = rawText.split("\n");

  // Find heading line (starts with ###)
  let heading: string | null = null;
  let contentStartIndex = 0;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    if (line.startsWith("### ")) {
      heading = line.substring(4).trim();
      contentStartIndex = i + 1;
      break;
    }
  }

  if (!heading) {
    return null;
  }

  // Parse key-value pairs
  const fields: Record<string, string> = {};

  for (let i = contentStartIndex; i < lines.length; i++) {
    const kvPair = parseMMLLine(lines[i]);
    if (kvPair) {
      const [key, value] = kvPair;
      fields[key] = value;
    }
  }

  // Validate required fields
  if (!fields.what || !fields.tags || !fields.importance || !fields.created) {
    return null;
  }

  // Parse tags (comma-separated)
  const tags = fields.tags
    .split(",")
    .map((t) => t.trim())
    .filter((t) => t.length > 0);

  // Parse importance
  const importance = parseFloat(fields.importance);
  if (isNaN(importance)) {
    return null;
  }

  // Generate ID
  const id = generateEntryId(heading, fields.created);

  // Build entry
  const entry: MemoryEntry = {
    id,
    title: heading,
    fields,
    what: fields.what,
    tags,
    importance,
    created: fields.created,
    layer,
    scope,
    filePath,
  };

  // Add optional fields for convenience
  if (fields.why) entry.why = fields.why;
  if (fields.rejected) entry.rejected = fields.rejected;
  if (fields.constraint) entry.constraint = fields.constraint;
  if (fields.do) entry.do = fields.do;
  if (fields.dont) entry.dont = fields.dont;
  if (fields.symptom) entry.symptom = fields.symptom;
  if (fields.fix) entry.fix = fields.fix;
  if (fields["root-cause"]) entry["root-cause"] = fields["root-cause"];
  if (fields.workaround) entry.workaround = fields.workaround;
  if (fields.file) entry.file = fields.file;
  if (fields.source) entry.source = fields.source as any;
  if (fields.updated) entry.updated = fields.updated;
  if (fields.decay_rate) entry.decay_rate = parseFloat(fields.decay_rate);
  if (fields.promoted_from) entry.promoted_from = fields.promoted_from as any;
  if (fields.acquisition) {
    const parsed = parseAcquisition(fields.acquisition);
    if (parsed) entry.acquisition = parsed;
  }

  return entry;
}

/**
 * Serialize entry to MML format
 */
export function serializeEntry(entry: MemoryEntry): string {
  const parts: string[] = [];

  // Heading
  parts.push(`### ${entry.title}`);

  // Always include required fields first
  parts.push(`- **what**: ${entry.what}`);

  // Add other fields from the fields dictionary (excluding what which we already added)
  const sortedKeys = Object.keys(entry.fields)
    .filter((k) => k !== "what")
    .sort((a, b) => {
      // Sort order: why, rejected, constraint, do, dont, symptom, fix, root-cause, workaround, file, tags, importance, created, others
      const priority: Record<string, number> = {
        why: 1,
        rejected: 2,
        constraint: 3,
        do: 4,
        dont: 5,
        symptom: 6,
        fix: 7,
        "root-cause": 8,
        workaround: 9,
        file: 10,
        tags: 96,
        importance: 97,
        created: 98,
        updated: 99,
        acquisition: 99.5,
      };
      return (priority[a] || 50) - (priority[b] || 50);
    });

  for (const key of sortedKeys) {
    parts.push(`- **${key}**: ${entry.fields[key]}`);
  }

  return parts.join("\n");
}

/**
 * Parse multiple entries from file content
 * Entries are separated by ### headings
 * Lines starting with # (but not ###) are treated as category headers and ignored
 */
export function parseEntries(
  fileContent: string,
  layer: MemoryLayer,
  scope: "project" | "global",
  filePath: string,
): MemoryEntry[] {
  const entries: MemoryEntry[] = [];
  const lines = fileContent.split("\n");

  let currentEntryLines: string[] = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const trimmed = line.trim();

    // ### marks the start of a new entry
    if (trimmed.startsWith("### ")) {
      // Parse previous entry if any
      if (currentEntryLines.length > 0) {
        const entry = parseEntry(
          currentEntryLines.join("\n"),
          layer,
          scope,
          filePath,
        );
        if (entry) {
          entries.push(entry);
        }
      }

      // Start new entry
      currentEntryLines = [line];
    } else if (trimmed.startsWith("# ") || trimmed.startsWith("## ")) {
      // H1/H2 headers are category headers, ignore them
      continue;
    } else if (currentEntryLines.length > 0) {
      // Add line to current entry
      currentEntryLines.push(line);
    }
  }

  // Parse last entry
  if (currentEntryLines.length > 0) {
    const entry = parseEntry(
      currentEntryLines.join("\n"),
      layer,
      scope,
      filePath,
    );
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
      parts.push(""); // Blank line between entries
    }
    parts.push(serializeEntry(entries[i]));
  }

  return parts.join("\n");
}

/**
 * Extract header from file content
 * Header is everything before the first ### entry
 */
export function extractHeader(fileContent: string): string {
  const lines = fileContent.split("\n");
  const headerLines: string[] = [];

  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith("### ")) {
      break;
    }
    headerLines.push(line);
  }

  return headerLines.join("\n").trim();
}
