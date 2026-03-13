/**
 * Configuration type definitions for memorykit.yaml
 */

/**
 * Working memory layer configuration
 */
export interface WorkingConfig {
  max_entries: number;
  decay_threshold_days: number;
  promotion_threshold: number;
}

/**
 * Facts memory layer configuration
 */
export interface FactsConfig {
  max_entries_per_file: number;
  auto_categorize: boolean;
}

/**
 * Episodes memory layer configuration
 */
export interface EpisodesConfig {
  date_format: string;
  compaction_after_days: number;
}

/**
 * Procedures memory layer configuration
 */
export interface ProceduresConfig {
  trigger_patterns: boolean;
}

/**
 * Consolidation configuration
 */
export interface ConsolidationConfig {
  auto: boolean;
  interval_minutes: number;
}

/**
 * Global memory configuration
 */
export interface GlobalConfig {
  enabled: boolean;
  priority: "project" | "global";
}

/**
 * Context retrieval configuration
 */
export interface ContextConfig {
  max_tokens_estimate: number;
  prioritize_by: "importance" | "recency" | "relevance";
}

/**
 * Quality gates configuration for memory storage
 */
export interface QualityGatesConfig {
  importance_floor: number;
  duplicate_jaccard_threshold: number;
  duplicate_word_overlap: number;
}

/**
 * Complete MemoryKit configuration
 */
export interface MemoryKitConfig {
  version: string;
  working: WorkingConfig;
  facts: FactsConfig;
  episodes: EpisodesConfig;
  procedures: ProceduresConfig;
  consolidation: ConsolidationConfig;
  global: GlobalConfig;
  context: ContextConfig;
  quality_gates?: QualityGatesConfig;
}

/**
 * Default configuration values
 */
export const DEFAULT_CONFIG: MemoryKitConfig = {
  version: "0.1",
  working: {
    max_entries: 50,
    decay_threshold_days: 7,
    promotion_threshold: 0.7,
  },
  facts: {
    max_entries_per_file: 100,
    auto_categorize: true,
  },
  episodes: {
    date_format: "YYYY-MM-DD",
    compaction_after_days: 30,
  },
  procedures: {
    trigger_patterns: true,
  },
  consolidation: {
    auto: true,
    interval_minutes: 0,
  },
  global: {
    enabled: true,
    priority: "project",
  },
  context: {
    max_tokens_estimate: 4000,
    prioritize_by: "importance",
  },
};
