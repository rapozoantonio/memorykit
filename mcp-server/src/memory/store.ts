/**
 * Store operation - Write memories with automatic importance scoring and routing
 */

import type {
  StoreResult,
  StoreOptions,
  MemoryEntry,
  MemoryScope,
  MemoryLayer,
} from "../types/memory.js";
import { MemoryScope as Scope, MemoryLayer as Layer } from "../types/memory.js";
import { calculateImportance } from "../cognitive/amygdala.js";
import { generateEntryId } from "../storage/entry-parser.js";
import { appendEntry, readMemoryFile } from "../storage/file-manager.js";
import {
  resolveFilePath,
  resolveLayerPath,
} from "../storage/scope-resolver.js";
import { loadConfig } from "../storage/config-loader.js";
import { consolidateMemory } from "./consolidate.js";
import { existsSync, readdirSync } from "fs";

// Consolidation debouncing with status tracking
let lastConsolidationTime = 0;
let lastConsolidationStatus: "none" | "success" | "failed" = "none";
const CONSOLIDATION_INTERVAL_MS = 5 * 60 * 1000; // 5 minutes

/**
 * Get last consolidation status (exported for status command)
 */
export function getConsolidationStatus() {
  return {
    lastRun: lastConsolidationTime,
    status: lastConsolidationStatus,
  };
}

/**
 * Store a memory entry
 */
export async function storeMemory(
  content: string,
  options: StoreOptions = {},
): Promise<StoreResult> {
  const config = loadConfig();

  // Auto-detect tags if not provided
  const tags = options.tags ?? autoDetectTags(content);

  // Read existing entries for context
  const scope = options.scope ?? Scope.Project;
  const existingEntries = await getExistingEntries(scope);

  // Calculate importance
  const importance = calculateImportance(content, {
    existingEntries,
    recentTags: tags,
  });

  // Determine layer (use provided or auto-detect)
  const layer = options.layer ?? determineLayer(content, importance, config);

  // Determine target file
  const filename = options.file_hint ?? determineFilename(layer, tags, content);
  const filePath = resolveFilePath(scope, layer, filename);

  // Create entry
  const entry: MemoryEntry = {
    id: generateEntryId(content),
    content,
    importance,
    created: new Date().toISOString(),
    tags,
    source: "conversation",
  };

  // Write entry
  await appendEntry(filePath, entry);

  // Debounced consolidation - fire and forget
  if (
    config.consolidation.auto &&
    Date.now() - lastConsolidationTime > CONSOLIDATION_INTERVAL_MS
  ) {
    lastConsolidationTime = Date.now();
    consolidateMemory({
      scope: scope === Scope.Project ? "project" : "global",
      dry_run: false,
    })
      .then(() => {
        lastConsolidationStatus = "success";
      })
      .catch((err: Error) => {
        lastConsolidationStatus = "failed";
        console.error("Background consolidation failed:", err);
      });
  }

  return {
    stored: true,
    layer,
    file: filename,
    importance,
    tags,
    entry_id: entry.id,
  };
}

/**
 * Auto-detect tags from content
 */
function autoDetectTags(content: string): string[] {
  const tags: Set<string> = new Set();

  // Technical domains
  const domains = [
    "database",
    "api",
    "frontend",
    "backend",
    "auth",
    "security",
    "testing",
    "deployment",
    "architecture",
    "design",
    "performance",
  ];

  const lower = content.toLowerCase();
  for (const domain of domains) {
    if (lower.includes(domain)) {
      tags.add(domain);
    }
  }

  // Programming languages
  const languages = [
    "typescript",
    "javascript",
    "python",
    "java",
    "csharp",
    "go",
    "rust",
  ];
  for (const lang of languages) {
    if (lower.includes(lang) || lower.includes(lang.replace(/script$/, ""))) {
      tags.add(lang);
    }
  }

  // Frameworks
  const frameworks = [
    "react",
    "vue",
    "angular",
    "express",
    "nextjs",
    "django",
    "flask",
  ];
  for (const fw of frameworks) {
    if (lower.includes(fw)) {
      tags.add(fw);
    }
  }

  return Array.from(tags);
}

/**
 * Determine layer based on content and importance
 */
function determineLayer(
  content: string,
  importance: number,
  config: any,
): MemoryLayer {
  const lower = content.toLowerCase();

  // Procedural indicators
  if (
    /\b(always|never|when|if|then|rule|pattern|best practice|convention)\b/i.test(
      content,
    ) ||
    lower.includes("how to") ||
    lower.includes("step by step")
  ) {
    return Layer.Procedures;
  }

  // Episodic indicators (time-based events)
  if (
    /\b(today|yesterday|last week|on|bug|error|fixed|solved|debugging)\b/i.test(
      content,
    ) ||
    lower.includes("discovered") ||
    lower.includes("found")
  ) {
    return Layer.Episodes;
  }

  // Facts indicators (stable knowledge)
  if (
    /\b(is|are|uses|has|technology|stack|framework|library|database)\b/i.test(
      content,
    ) ||
    importance > 0.7
  ) {
    return Layer.Facts;
  }

  // Default: working memory
  return Layer.Working;
}

/**
 * Determine filename within layer
 */
function determineFilename(
  layer: MemoryLayer,
  tags: string[],
  content: string,
): string {
  switch (layer) {
    case Layer.Working:
      return "session.md";

    case Layer.Facts:
      // Categorize by primary tag or content
      if (tags.includes("architecture") || tags.includes("design")) {
        return "architecture.md";
      }
      if (tags.includes("database") || tags.includes("api")) {
        return "technology.md";
      }
      if (
        content.toLowerCase().includes("team") ||
        content.toLowerCase().includes("process")
      ) {
        return "team.md";
      }
      return "general.md";

    case Layer.Episodes:
      // One file per day
      const date = new Date().toISOString().split("T")[0];
      return `${date}.md`;

    case Layer.Procedures:
      // Categorize by type
      if (
        content.toLowerCase().includes("code") ||
        content.toLowerCase().includes("style")
      ) {
        return "code-style.md";
      }
      if (
        content.toLowerCase().includes("debug") ||
        content.toLowerCase().includes("troubleshoot")
      ) {
        return "debugging.md";
      }
      if (
        content.toLowerCase().includes("deploy") ||
        content.toLowerCase().includes("release")
      ) {
        return "workflows.md";
      }
      return "general.md";
  }
}

/**
 * Get existing entries for context
 */
async function getExistingEntries(scope: MemoryScope): Promise<any[]> {
  try {
    const layerPath = resolveLayerPath(scope, Layer.Facts);
    if (!existsSync(layerPath)) {
      return [];
    }

    const files = readdirSync(layerPath).filter((f) => f.endsWith(".md"));
    const entries: any[] = [];

    // Process files in parallel
    const filePromises = files.slice(0, 3).map(async (file) => {
      const filePath = resolveFilePath(scope, Layer.Facts, file);
      return await readMemoryFile(filePath);
    });

    const results = await Promise.all(filePromises);
    for (const result of results) {
      entries.push(...result);
    }

    return entries;
  } catch {
    return [];
  }
}
