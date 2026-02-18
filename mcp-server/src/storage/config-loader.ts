/**
 * Config loader - Load and validate memorykit.yaml configuration
 */

import { readFileSync, existsSync } from "fs";
import { parse as parseYaml } from "yaml";
import type { MemoryKitConfig } from "../types/config.js";
import { DEFAULT_CONFIG } from "../types/config.js";
import {
  getConfigPath,
  resolveProjectRoot,
  resolveGlobalRoot,
} from "./scope-resolver.js";
import { MemoryScope } from "../types/memory.js";

/**
 * Load configuration from a file
 */
export function loadConfigFile(
  configPath: string,
): Partial<MemoryKitConfig> | null {
  if (!existsSync(configPath)) {
    return null;
  }

  try {
    const content = readFileSync(configPath, "utf-8");
    return parseYaml(content) as Partial<MemoryKitConfig>;
  } catch (error) {
    console.error(`Failed to parse config ${configPath}:`, error);
    return null;
  }
}

/**
 * Merge two configs with priority to the first one
 */
export function mergeConfigs(
  primary: Partial<MemoryKitConfig>,
  secondary: Partial<MemoryKitConfig>,
): MemoryKitConfig {
  return {
    version: primary.version ?? secondary.version ?? DEFAULT_CONFIG.version,
    working: {
      ...DEFAULT_CONFIG.working,
      ...secondary.working,
      ...primary.working,
    },
    facts: {
      ...DEFAULT_CONFIG.facts,
      ...secondary.facts,
      ...primary.facts,
    },
    episodes: {
      ...DEFAULT_CONFIG.episodes,
      ...secondary.episodes,
      ...primary.episodes,
    },
    procedures: {
      ...DEFAULT_CONFIG.procedures,
      ...secondary.procedures,
      ...primary.procedures,
    },
    consolidation: {
      ...DEFAULT_CONFIG.consolidation,
      ...secondary.consolidation,
      ...primary.consolidation,
    },
    global: {
      ...DEFAULT_CONFIG.global,
      ...secondary.global,
      ...primary.global,
    },
    context: {
      ...DEFAULT_CONFIG.context,
      ...secondary.context,
      ...primary.context,
    },
  };
}

/**
 * Load configuration with project and global scope merging
 */
export function loadConfig(): MemoryKitConfig {
  // Try loading project config
  const projectConfigPath = getConfigPath(MemoryScope.Project);
  const projectConfig = loadConfigFile(projectConfigPath) ?? {};

  // Try loading global config
  const globalConfigPath = getConfigPath(MemoryScope.Global);
  const globalConfig = loadConfigFile(globalConfigPath) ?? {};

  // Merge: project takes priority over global, both over default
  return mergeConfigs(
    projectConfig,
    mergeConfigs(globalConfig, DEFAULT_CONFIG),
  );
}

/**
 * Get default configuration
 */
export function getDefaultConfig(): MemoryKitConfig {
  return { ...DEFAULT_CONFIG };
}

/**
 * Validate configuration (basic checks)
 */
export function validateConfig(config: MemoryKitConfig): {
  valid: boolean;
  errors: string[];
} {
  const errors: string[] = [];

  // Check version
  if (!config.version) {
    errors.push("Missing version field");
  }

  // Check working config
  if (config.working.max_entries < 1) {
    errors.push("working.max_entries must be >= 1");
  }
  if (config.working.decay_threshold_days < 1) {
    errors.push("working.decay_threshold_days must be >= 1");
  }
  if (
    config.working.promotion_threshold < 0 ||
    config.working.promotion_threshold > 1
  ) {
    errors.push("working.promotion_threshold must be between 0 and 1");
  }

  // Check facts config
  if (config.facts.max_entries_per_file < 1) {
    errors.push("facts.max_entries_per_file must be >= 1");
  }

  // Check episodes config
  if (config.episodes.compaction_after_days < 1) {
    errors.push("episodes.compaction_after_days must be >= 1");
  }

  // Check consolidation config
  if (config.consolidation.interval_minutes < 0) {
    errors.push("consolidation.interval_minutes must be >= 0");
  }

  // Check context config
  if (config.context.max_tokens_estimate < 100) {
    errors.push("context.max_tokens_estimate must be >= 100");
  }

  return {
    valid: errors.length === 0,
    errors,
  };
}
