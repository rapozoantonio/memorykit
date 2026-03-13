/**
 * Retrieve operation - Read relevant memory based on query classification
 */

import type {
  RetrieveResult,
  RetrieveOptions,
  MemoryEntry,
} from "../types/memory.js";
import { MemoryScope } from "../types/memory.js";
import { MemoryLayer } from "../types/memory.js";
import { classifyQuery, resolveFiles } from "../cognitive/prefrontal.js";
import { calculateEffectiveScore } from "../cognitive/amygdala.js";
import { CommonWords } from "../cognitive/patterns.js";
import { readMemoryFile, listMemoryFiles } from "../storage/file-manager.js";
import { serializeEntry } from "../storage/entry-parser.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
} from "../storage/scope-resolver.js";
import { loadConfig } from "../storage/config-loader.js";
import { join } from "path";
import { existsSync } from "fs";
import { glob } from "glob";

/**
 * ROI statistics for retrieval
 */
interface ROIStats {
  entries_with_acquisition: number;
  total_acquisition_tokens: number;
  total_acquisition_tool_calls: number;
  retrieval_tokens: number;
  tokens_saved: number;
  efficiency_percent: number;
  is_estimated?: boolean;
}

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
  let filesToRead = resolveFiles(classification, config);

  // Filter file patterns to only requested layers (if specified)
  if (options.layers && options.layers.length > 0) {
    const requestedLayers = new Set<string>(options.layers);
    filesToRead = {
      project: filesToRead.project.filter((p) =>
        requestedLayers.has(p.split("/")[0]),
      ),
      global: filesToRead.global.filter((p) =>
        requestedLayers.has(p.split("/")[0]),
      ),
    };
  }

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

  // Sort by relevance × effective score (importance × recency)
  const scoredEntries = sortByRelevanceScore(allEntries, query);

  // Filter by relevance threshold to exclude noise (min 20% query overlap)
  const MIN_RELEVANCE_SCORE = 0.2;
  const relevantEntries = scoredEntries
    .filter((s) => s.relevance >= MIN_RELEVANCE_SCORE)
    .map((s) => s.entry);

  // Truncate to token budget
  const { entries, tokenCount } = truncateToTokenBudget(
    relevantEntries,
    maxTokens,
  );

  // Compute ROI stats (only for relevant entries)
  const roiStats = computeROIStats(entries, tokenCount);

  // Format as MML-grouped markdown
  const context = formatAsMMContext(
    entries,
    options.scope ?? "project",
    tokenCount,
    maxTokens,
    allEntries.length,
    roiStats,
  );

  return {
    query_type: classification.type,
    confidence: classification.confidence,
    files_read: [...filesToRead.project, ...filesToRead.global],
    context,
    token_estimate: tokenCount,
    entries_returned: entries.length,
    entries_available: allEntries.length,
    roi_stats: {
      tokens_saved: roiStats.tokens_saved,
      tool_calls_saved: roiStats.total_acquisition_tool_calls,
      efficiency_percent: roiStats.efficiency_percent,
      is_estimated: roiStats.is_estimated ?? false,
    },
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
 * Tokenize text into lowercase words, filtering common/stop words
 */
function tokenize(text: string): Set<string> {
  const words = text
    .toLowerCase()
    .split(/[\s,.\!?;:\t\n\-_]+/)
    .filter((w) => w.length > 2);
  return new Set(words.filter((w) => !CommonWords.has(w)));
}

/**
 * Calculate relevance score between query tokens and entry tokens (0.1 - 1.0)
 */
function calculateRelevance(
  queryTokens: Set<string>,
  entryTokens: Set<string>,
): number {
  if (queryTokens.size === 0) return 1.0; // No query = all equally relevant

  let overlap = 0;
  for (const token of queryTokens) {
    if (entryTokens.has(token)) {
      overlap++;
    } else {
      // Partial match: substring containment
      for (const entryToken of entryTokens) {
        if (entryToken.includes(token) || token.includes(entryToken)) {
          overlap += 0.5;
          break;
        }
      }
    }
  }

  return Math.max(0.1, overlap / queryTokens.size);
}

/**
 * Sort entries by combined relevance × effective score
 * Pre-computes tokens to avoid O(n² log n) in sort comparator
 * Returns entries with relevance scores attached for filtering
 */
function sortByRelevanceScore(
  entries: MemoryEntry[],
  query: string,
): Array<{ entry: MemoryEntry; relevance: number; score: number }> {
  const queryTokens = tokenize(query);

  // Pre-compute tokens and scores for all entries
  const scored = entries.map((entry) => {
    const entryText = [entry.title, entry.what, ...entry.tags].join(" ");
    const entryTokens = tokenize(entryText);
    const relevance = calculateRelevance(queryTokens, entryTokens);
    const effective = calculateEffectiveScore(
      entry.importance,
      entry.created,
      entry.decay_rate,
    );

    return {
      entry,
      relevance, // Keep relevance for filtering
      score: relevance * effective,
    };
  });

  // Sort by pre-computed score
  scored.sort((a, b) => b.score - a.score);

  return scored;
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
    const entryTokens = estimateTokens(serializeEntry(entry));

    if (tokenCount + entryTokens > maxTokens) {
      break; // Budget exceeded
    }

    selected.push(entry);
    tokenCount += entryTokens;
  }

  return { entries: selected, tokenCount };
}

/**
 * Estimate tokens for text (simple heuristic: ~4 chars per token)
 */
function estimateTokens(text: string): number {
  return Math.ceil(text.length / 4);
}

/**
 * Estimate savings when acquisition context is unavailable
 * Heuristic: each entry represents ~2 tool calls (search + read) and ~500 tokens to discover
 */
function estimateSavingsWithoutAcquisition(
  entryCount: number,
  retrievalTokens: number,
): ROIStats {
  const ESTIMATED_TOKENS_PER_DISCOVERY = 500;
  const ESTIMATED_TOOL_CALLS_PER_DISCOVERY = 2;

  const estimatedAcquisitionTokens =
    entryCount * ESTIMATED_TOKENS_PER_DISCOVERY;
  const estimatedToolCalls = entryCount * ESTIMATED_TOOL_CALLS_PER_DISCOVERY;
  const tokensSaved = Math.max(0, estimatedAcquisitionTokens - retrievalTokens);
  const efficiencyPercent =
    estimatedAcquisitionTokens > 0
      ? Math.max(
          0,
          Math.round((tokensSaved / estimatedAcquisitionTokens) * 100),
        )
      : 0;

  return {
    entries_with_acquisition: 0,
    total_acquisition_tokens: estimatedAcquisitionTokens,
    total_acquisition_tool_calls: estimatedToolCalls,
    retrieval_tokens: retrievalTokens,
    tokens_saved: tokensSaved,
    efficiency_percent: efficiencyPercent,
    is_estimated: true,
  };
}

/**
 * Compute ROI statistics from retrieved entries
 */
function computeROIStats(
  entries: MemoryEntry[],
  retrievalTokens: number,
): ROIStats {
  let entriesWithAcquisition = 0;
  let totalAcquisitionTokens = 0;
  let totalAcquisitionToolCalls = 0;

  for (const entry of entries) {
    if (entry.acquisition) {
      entriesWithAcquisition++;
      totalAcquisitionTokens += entry.acquisition.tokens_consumed;
      totalAcquisitionToolCalls += entry.acquisition.tool_calls;
    }
  }

  // If NO entries have acquisition data, use heuristic estimate
  if (entriesWithAcquisition === 0 && entries.length > 0) {
    return estimateSavingsWithoutAcquisition(entries.length, retrievalTokens);
  }

  const tokensSaved = totalAcquisitionTokens - retrievalTokens;
  const efficiencyPercent =
    totalAcquisitionTokens > 0
      ? Math.round((1 - retrievalTokens / totalAcquisitionTokens) * 100)
      : 0;

  return {
    entries_with_acquisition: entriesWithAcquisition,
    total_acquisition_tokens: totalAcquisitionTokens,
    total_acquisition_tool_calls: totalAcquisitionToolCalls,
    retrieval_tokens: retrievalTokens,
    tokens_saved: tokensSaved,
    efficiency_percent: efficiencyPercent,
  };
}

/**
 * Format entries as MML-grouped context
 */
function formatAsMMContext(
  entries: MemoryEntry[],
  scope: string,
  tokenCount: number,
  maxTokens: number,
  totalAvailable: number,
  roiStats: ROIStats,
): string {
  const lines: string[] = [];

  if (entries.length === 0) {
    lines.push(`# Memory Context`);
    lines.push("");
    lines.push("No relevant memories found for this query.");
    return lines.join("\n");
  }

  // H1 header with metadata
  lines.push(
    `# Memory Context (${scope}, ${entries.length} entries, ~${tokenCount} tokens)`,
  );
  lines.push("");

  // Group by layer and scope
  const projectEntries = entries.filter((e) => e.scope === "project");
  const globalEntries = entries.filter((e) => e.scope === "global");

  // Process project entries by layer
  if (projectEntries.length > 0) {
    appendLayerGroups(lines, projectEntries);
  }

  // Process global entries separately
  if (globalEntries.length > 0) {
    lines.push("---");
    lines.push("[global]");
    lines.push("");
    appendLayerGroups(lines, globalEntries);
  }

  // Add truncation notification if needed
  const truncated = totalAvailable - entries.length;
  if (truncated > 0) {
    lines.push("");
    lines.push(
      `[+${truncated} more entries available, increase max_tokens to see]`,
    );
  }

  return lines.join("\n");
}

/**
 * Append entries grouped by layer
 */
function appendLayerGroups(lines: string[], entries: MemoryEntry[]): void {
  // Group by layer
  const byLayer: Record<string, MemoryEntry[]> = {};

  for (const entry of entries) {
    const layer = entry.layer || "Working";
    if (!byLayer[layer]) {
      byLayer[layer] = [];
    }
    byLayer[layer].push(entry);
  }

  // Output in order: Facts → Procedures → Episodes → Working
  const layerOrder: MemoryLayer[] = [
    MemoryLayer.Facts,
    MemoryLayer.Procedures,
    MemoryLayer.Episodes,
    MemoryLayer.Working,
  ];

  for (const layer of layerOrder) {
    const layerEntries = byLayer[layer];
    if (!layerEntries || layerEntries.length === 0) continue;

    // Sort by effective score within layer
    layerEntries.sort((a, b) => {
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
      return scoreB - scoreA;
    });

    // H2 layer heading
    lines.push(`## ${layer}`);
    lines.push("");

    // Format each entry (strip importance/created)
    for (const entry of layerEntries) {
      lines.push(`### ${entry.title}`);

      // Output fields except importance, created, and internal metadata
      const skipKeys = new Set([
        "importance",
        "created",
        "updated",
        "decay_rate",
        "source",
        "acquisition",
      ]);

      // Always output 'what' first
      lines.push(`- **what**: ${entry.what}`);

      // Then other fields in priority order
      const sortedKeys = Object.keys(entry.fields)
        .filter((k) => k !== "what" && k !== "tags" && !skipKeys.has(k))
        .sort((a, b) => {
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
          };
          return (priority[a] || 50) - (priority[b] || 50);
        });

      for (const key of sortedKeys) {
        lines.push(`- **${key}**: ${entry.fields[key]}`);
      }

      // Tags always last
      lines.push(`- **tags**: ${entry.tags.join(", ")}`);
      lines.push("");
    }
  }
}

/**
 * Format tags for display
 */
function formatTags(tags: string[]): string {
  if (tags.length === 0) return "General";
  return tags.map((t) => `#${t}`).join(" ");
}
