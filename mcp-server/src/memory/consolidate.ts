/**
 * Consolidate operation - Memory maintenance (prune, promote, compact)
 */

import type {
  ConsolidateResult,
  ConsolidateOptions,
  ConsolidationAction,
  MemoryEntry,
  MemoryLayer,
  MemoryScope,
} from "../types/memory.js";
import { MemoryScope as Scope, MemoryLayer as Layer } from "../types/memory.js";
import {
  readMemoryFile,
  writeMemoryFile,
  removeEntry,
  appendEntry,
  listMemoryFiles,
} from "../storage/file-manager.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
  resolveFilePath,
} from "../storage/scope-resolver.js";
import { loadConfig } from "../storage/config-loader.js";
import { extractHeader } from "../storage/entry-parser.js";
import { readFile } from "fs/promises";
import { existsSync } from "fs";

/**
 * Consolidate memory - Run maintenance on working memory
 */
export async function consolidateMemory(
  options: ConsolidateOptions = {},
): Promise<ConsolidateResult> {
  const config = loadConfig();
  const scope = options.scope ?? "project";
  const dryRun = options.dry_run ?? false;

  const actions: ConsolidationAction[] = [];
  let pruned = 0;
  let promoted = 0;
  let compacted = 0;

  // Determine which roots to process
  const roots: { root: string; scope: MemoryScope }[] = [];
  if (scope === "project" || scope === "all") {
    roots.push({ root: resolveProjectRoot(), scope: Scope.Project });
  }
  if (scope === "global" || scope === "all") {
    roots.push({ root: resolveGlobalRoot(), scope: Scope.Global });
  }

  for (const { root, scope: memScope } of roots) {
    if (!existsSync(root)) continue;

    // Rule 1 & 2: Process working memory (prune old, promote high-importance)
    const workingResult = await processWorkingMemory(
      root,
      memScope,
      config,
      dryRun,
    );
    actions.push(...workingResult.actions);
    pruned += workingResult.pruned;
    promoted += workingResult.promoted;

    // Rule 3: Compact old episodes
    const episodesResult = await compactEpisodes(
      root,
      memScope,
      config,
      dryRun,
    );
    actions.push(...episodesResult.actions);
    compacted += episodesResult.compacted;

    // Rule 4: Enforce working memory size cap
    const capResult = await enforceWorkingCap(root, memScope, config, dryRun);
    actions.push(...capResult.actions);
    pruned += capResult.pruned;
  }

  return {
    pruned,
    promoted,
    compacted,
    details: actions,
  };
}

/**
 * Process working memory: prune old entries, promote high-importance
 */
async function processWorkingMemory(
  root: string,
  scope: MemoryScope,
  config: any,
  dryRun: boolean,
): Promise<{
  actions: ConsolidationAction[];
  pruned: number;
  promoted: number;
}> {
  const actions: ConsolidationAction[] = [];
  let pruned = 0;
  let promoted = 0;

  const workingPath = resolveFilePath(scope, Layer.Working, "session.md");
  if (!existsSync(workingPath)) {
    return { actions, pruned, promoted };
  }

  const entries = await readMemoryFile(workingPath);
  const now = new Date();
  const thresholdMs = config.working.decay_threshold_days * 24 * 60 * 60 * 1000;

  const toKeep: MemoryEntry[] = [];
  const toPromote: MemoryEntry[] = [];
  const toPrune: MemoryEntry[] = [];

  for (const entry of entries) {
    const age = now.getTime() - new Date(entry.created).getTime();

    // High importance → promote
    if (entry.importance >= config.working.promotion_threshold) {
      toPromote.push(entry);
      actions.push({
        action: "promoted",
        entry_id: entry.id,
        from: Layer.Working,
        to: determinePromotionTarget(entry),
        importance: entry.importance,
      });
      promoted++;
    }
    // Old and low importance → prune
    else if (
      age > thresholdMs &&
      entry.importance < config.working.promotion_threshold
    ) {
      toPrune.push(entry);
      actions.push({
        action: "pruned",
        entry_id: entry.id,
        reason: `low importance (${entry.importance.toFixed(2)}), ${Math.floor(age / (24 * 60 * 60 * 1000))} days old`,
      });
      pruned++;
    }
    // Keep in working memory
    else {
      toKeep.push(entry);
    }
  }

  if (!dryRun) {
    // Update working memory file
    if (toKeep.length > 0 || toPromote.length > 0) {
      const content = await readFile(workingPath, "utf-8");
      const header = extractHeader(content);
      await writeMemoryFile(workingPath, toKeep, header);
    }

    // Promote entries in parallel
    await Promise.all(
      toPromote.map(async (entry) => {
        const targetLayer = determinePromotionTarget(entry);
        const targetFile = determineTargetFile(targetLayer, entry);
        const targetPath = resolveFilePath(scope, targetLayer, targetFile);

        const promotedEntry = {
          ...entry,
          promoted_from: Layer.Working,
        };

        await appendEntry(targetPath, promotedEntry);
      }),
    );
  }

  return { actions, pruned, promoted };
}

/**
 * Determine where to promote an entry
 */
function determinePromotionTarget(entry: MemoryEntry): Layer {
  const content = entry.content.toLowerCase();

  // Procedural patterns
  if (/\b(always|never|when|rule|pattern)\b/.test(content)) {
    return Layer.Procedures;
  }

  // Episodic patterns
  if (/\b(bug|error|fixed|discovered|found)\b/.test(content)) {
    return Layer.Episodes;
  }

  // Default: facts
  return Layer.Facts;
}

/**
 * Determine target filename for promotion
 */
function determineTargetFile(layer: Layer, entry: MemoryEntry): string {
  switch (layer) {
    case Layer.Facts:
      if (entry.tags.includes("architecture")) return "architecture.md";
      if (entry.tags.includes("technology")) return "technology.md";
      return "general.md";
    case Layer.Episodes:
      return `${new Date(entry.created).toISOString().split("T")[0]}.md`;
    case Layer.Procedures:
      if (entry.content.toLowerCase().includes("code")) return "code-style.md";
      if (entry.content.toLowerCase().includes("debug")) return "debugging.md";
      return "general.md";
    default:
      return "general.md";
  }
}

/**
 * Compact old episodes
 */
async function compactEpisodes(
  root: string,
  scope: MemoryScope,
  config: any,
  dryRun: boolean,
): Promise<{ actions: ConsolidationAction[]; compacted: number }> {
  const actions: ConsolidationAction[] = [];
  let compacted = 0;

  const files = await listMemoryFiles(root);
  const episodeFiles = files.filter((f) => f.layer === Layer.Episodes);
  const now = new Date();
  const thresholdMs =
    config.episodes.compaction_after_days * 24 * 60 * 60 * 1000;

  for (const fileInfo of episodeFiles) {
    const entries = await readMemoryFile(fileInfo.path);
    let modified = false;

    const updatedEntries = entries.map((entry) => {
      const age = now.getTime() - new Date(entry.created).getTime();

      // Old and low importance → compact (truncate content)
      if (age > thresholdMs && entry.importance < 0.6) {
        modified = true;
        compacted++;
        actions.push({
          action: "compacted",
          entry_id: entry.id,
          reason: `${Math.floor(age / (24 * 60 * 60 * 1000))} days old`,
        });

        // Truncate to first 2 sentences
        const sentences = entry.content.split(/[.!?]+/).filter((s) => s.trim());
        const truncated = sentences.slice(0, 2).join(". ") + ".";

        return {
          ...entry,
          content: truncated,
          source: "consolidation" as const,
        };
      }

      return entry;
    });

    if (modified && !dryRun) {
      const content = await readFile(fileInfo.path, "utf-8");
      const header = extractHeader(content);
      await writeMemoryFile(fileInfo.path, updatedEntries, header);
    }
  }

  return { actions, compacted };
}

/**
 * Enforce working memory size cap
 */
async function enforceWorkingCap(
  root: string,
  scope: MemoryScope,
  config: any,
  dryRun: boolean,
): Promise<{ actions: ConsolidationAction[]; pruned: number }> {
  const actions: ConsolidationAction[] = [];
  let pruned = 0;

  const workingPath = resolveFilePath(scope, Layer.Working, "session.md");
  if (!existsSync(workingPath)) {
    return { actions, pruned };
  }

  const entries = await readMemoryFile(workingPath);

  if (entries.length <= config.working.max_entries) {
    return { actions, pruned };
  }

  // Sort by importance (ascending) and prune lowest
  const sorted = entries.sort((a, b) => a.importance - b.importance);
  const toRemove = entries.length - config.working.max_entries;

  const toKeep: MemoryEntry[] = [];
  const toPromote: MemoryEntry[] = [];

  for (let i = 0; i < sorted.length; i++) {
    if (i < toRemove) {
      // Check if worth promoting
      if (sorted[i].importance > 0.5) {
        toPromote.push(sorted[i]);
        actions.push({
          action: "promoted",
          entry_id: sorted[i].id,
          from: Layer.Working,
          to: determinePromotionTarget(sorted[i]),
          importance: sorted[i].importance,
        });
      } else {
        actions.push({
          action: "pruned",
          entry_id: sorted[i].id,
          reason: `working memory at capacity (${entries.length}/${config.working.max_entries})`,
        });
        pruned++;
      }
    } else {
      toKeep.push(sorted[i]);
    }
  }

  if (!dryRun) {
    const content = await readFile(workingPath, "utf-8");
    const header = extractHeader(content);
    await writeMemoryFile(workingPath, toKeep, header);

    // Promote entries in parallel
    await Promise.all(
      toPromote.map(async (entry) => {
        const targetLayer = determinePromotionTarget(entry);
        const targetFile = determineTargetFile(targetLayer, entry);
        const targetPath = resolveFilePath(scope, targetLayer, targetFile);

        await appendEntry(targetPath, {
          ...entry,
          promoted_from: Layer.Working,
        });
      }),
    );
  }

  return { actions, pruned };
}
