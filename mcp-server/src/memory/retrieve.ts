/**
 * Retrieve operation - Read relevant memory based on query classification
 */

import type {
  RetrieveResult,
  RetrieveOptions,
  MemoryEntry,
} from "../types/memory.js";
import { MemoryScope } from "../types/memory.js";
import { classifyQuery, resolveFiles } from "../cognitive/prefrontal.js";
import { calculateEffectiveScore } from "../cognitive/amygdala.js";
import { readMemoryFile, listMemoryFiles } from "../storage/file-manager.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
} from "../storage/scope-resolver.js";
import { loadConfig } from "../storage/config-loader.js";
import { join } from "path";
import { existsSync } from "fs";
import { glob } from "glob";

/**
 * Retrieve memory context for a query
 */
export async function retrieveContext(
  query: string,
  options: RetrieveOptions = {},
): Promise<RetrieveResult> {
  const config = loadConfig();

  // Classify query
  const classification = classifyQuery(query);

  // Resolve which files to read
  const filesToRead = resolveFiles(classification, config);

  // Determine token budget
  const maxTokens = options.max_tokens ?? config.context.max_tokens_estimate;

  // Collect entries from both scopes
  const projectEntries = await collectEntries(
    resolveProjectRoot(),
    filesToRead.project,
    options.scope !== "global",
  );

  const globalEntries = await collectEntries(
    resolveGlobalRoot(),
    filesToRead.global,
    options.scope !== "project" && config.global.enabled,
  );

  // Merge entries (project takes priority)
  const allEntries = mergeEntries(
    projectEntries,
    globalEntries,
    config.global.priority,
  );

  // Sort by effective score (importance × recency)
  const sortedEntries = sortByEffectiveScore(allEntries);

  // Truncate to token budget
  const { entries, tokenCount } = truncateToTokenBudget(
    sortedEntries,
    maxTokens,
  );

  // Format as markdown
  const context = formatAsMarkdown(entries, query, classification.type);

  return {
    query_type: classification.type,
    confidence: classification.confidence,
    files_read: [...filesToRead.project, ...filesToRead.global],
    context,
    token_estimate: tokenCount,
    entries_returned: entries.length,
    entries_available: allEntries.length,
  };
}

/**
 * Collect entries from file patterns
 */
async function collectEntries(
  rootPath: string,
  patterns: string[],
  enabled: boolean,
): Promise<MemoryEntry[]> {
  if (!enabled || !existsSync(rootPath)) {
    return [];
  }

  const entries: MemoryEntry[] = [];

  for (const pattern of patterns) {
    // Expand glob pattern constrained to rootPath
    const matches = await glob(pattern, {
      cwd: rootPath,
      nodir: true,
      absolute: true,
    });

    for (const filePath of matches) {
      const fileEntries = await readMemoryFile(filePath);
      entries.push(...fileEntries);
    }
  }

  return entries;
}

/**
 * Merge project and global entries
 */
function mergeEntries(
  projectEntries: MemoryEntry[],
  globalEntries: MemoryEntry[],
  priority: "project" | "global",
): MemoryEntry[] {
  if (priority === "project") {
    // Project entries first, then global
    return [...projectEntries, ...globalEntries];
  } else {
    // Global entries first, then project
    return [...globalEntries, ...projectEntries];
  }
}

/**
 * Sort entries by effective score
 */
function sortByEffectiveScore(entries: MemoryEntry[]): MemoryEntry[] {
  return entries.sort((a, b) => {
    const scoreA = calculateEffectiveScore(
      a.importance,
      a.created,
      a.decay_rate,
    );
    const scoreB = calculateEffectiveScore(
      b.importance,
      b.created,
      b.decay_rate,
    );
    return scoreB - scoreA; // Descending order
  });
}

/**
 * Truncate entries to fit token budget
 */
function truncateToTokenBudget(
  entries: MemoryEntry[],
  maxTokens: number,
): { entries: MemoryEntry[]; tokenCount: number } {
  const selected: MemoryEntry[] = [];
  let tokenCount = 0;

  for (const entry of entries) {
    const entryTokens = estimateTokens(entry.content);

    if (tokenCount + entryTokens > maxTokens) {
      break; // Budget exceeded
    }

    selected.push(entry);
    tokenCount += entryTokens;
  }

  return { entries: selected, tokenCount };
}

/**
 * Estimate tokens for text (simple heuristic)
 */
function estimateTokens(text: string): number {
  // Average: ~3.5 characters per token
  return Math.ceil(text.length / 3.5);
}

/**
 * Format entries as markdown
 */
function formatAsMarkdown(
  entries: MemoryEntry[],
  query: string,
  queryType: string,
): string {
  if (entries.length === 0) {
    return `# Memory Context\n\nNo relevant memories found for: "${query}"`;
  }

  const lines: string[] = [];
  lines.push(`# Memory Context`);
  lines.push("");
  lines.push(`Query: "${query}" (Type: ${queryType})`);
  lines.push("");
  lines.push("---");
  lines.push("");

  // Group by tags
  const grouped = groupByTags(entries);

  for (const [tag, tagEntries] of Object.entries(grouped)) {
    if (tag) {
      lines.push(`## ${tag}`);
      lines.push("");
    }

    for (const entry of tagEntries) {
      lines.push(`### ${formatTags(entry.tags)}`);
      lines.push("");
      lines.push(entry.content);
      lines.push("");
      lines.push(
        `*Importance: ${entry.importance.toFixed(2)} | Created: ${new Date(entry.created).toLocaleDateString()}*`,
      );
      lines.push("");
      lines.push("---");
      lines.push("");
    }
  }

  return lines.join("\n");
}

/**
 * Group entries by primary tag
 */
function groupByTags(entries: MemoryEntry[]): Record<string, MemoryEntry[]> {
  const grouped: Record<string, MemoryEntry[]> = { "": [] };

  for (const entry of entries) {
    const primaryTag = entry.tags[0] || "";
    if (!grouped[primaryTag]) {
      grouped[primaryTag] = [];
    }
    grouped[primaryTag].push(entry);
  }

  return grouped;
}

/**
 * Format tags for display
 */
function formatTags(tags: string[]): string {
  if (tags.length === 0) return "General";
  return tags.map((t) => `#${t}`).join(" ");
}
