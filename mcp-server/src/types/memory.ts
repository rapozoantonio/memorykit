/**
 * Memory type definitions for MemoryKit file-based storage
 */

/**
 * Memory layers following neuroscience model
 */
export enum MemoryLayer {
  Working = "working",
  Facts = "facts",
  Episodes = "episodes",
  Procedures = "procedures",
}

/**
 * Acquisition context - tracks the cost of discovering this knowledge
 */
export interface AcquisitionContext {
  /** Total tokens consumed to acquire this knowledge */
  tokens_consumed: number;
  /** Number of tool calls (searches, file reads) that produced this knowledge */
  tool_calls: number;
}

/**
 * Memory scope: project-specific or global
 */
export enum MemoryScope {
  Project = "project",
  Global = "global",
}

/**
 * Source of a memory entry
 */
export type MemorySource = "conversation" | "manual" | "consolidation";

/**
 * A single memory entry in MML (Markdown Memory Language) format
 */
export interface MemoryEntry {
  /** Unique identifier: generated from heading hash + created timestamp */
  id: string;
  /** Entry title from ### heading */
  title: string;
  /** All key-value pairs from - **key**: value lines */
  fields: Record<string, string>;
  /** Required: what field (extracted from fields for convenience) */
  what: string;
  /** Required: categorization tags (parsed from comma-separated string) */
  tags: string[];
  /** Required: Amygdala-calculated importance score (0.0-1.0) */
  importance: number;
  /** Required: ISO 8601 date string (YYYY-MM-DD or full timestamp) */
  created: string;
  /** Memory layer (inferred from file path) */
  layer: MemoryLayer;
  /** Memory scope (inferred from root path) */
  scope: "project" | "global";
  /** Source file path */
  filePath: string;

  // Optional MML fields (layer-specific)
  /** Why field (common in decisions/facts) */
  why?: string;
  /** Rejected alternatives (decisions) */
  rejected?: string;
  /** Constraints (decisions) */
  constraint?: string;
  /** Do field (procedures) */
  do?: string;
  /** Don't field (procedures) */
  dont?: string;
  /** Symptom field (episodes/bugs) */
  symptom?: string;
  /** Fix field (episodes/bugs) */
  fix?: string;
  /** Root cause field (episodes) */
  "root-cause"?: string;
  /** Workaround field (episodes) */
  workaround?: string;
  /** File reference (episodes) */
  file?: string;
  /** Source of memory (for backward compatibility) */
  source?: MemorySource;
  /** Last modification timestamp (optional) */
  updated?: string;
  /** Custom decay rate override (optional) */
  decay_rate?: number;
  /** Original layer if promoted (optional) */
  promoted_from?: MemoryLayer;
  /** Acquisition context - cost to produce this knowledge (internal only) */
  acquisition?: AcquisitionContext;
}

/**
 * Metadata extracted from entry blockquote line
 */
export interface EntryMetadata {
  importance: number;
  created: string;
  tags: string[];
  source: MemorySource;
  updated?: string;
  decay_rate?: number;
  promoted_from?: string;
}

/**
 * A memory file containing multiple entries
 */
export interface MemoryFile {
  /** Memory layer this file belongs to */
  layer: MemoryLayer;
  /** Filename (without path) */
  filename: string;
  /** Full file path */
  path: string;
  /** Parsed entries */
  entries: MemoryEntry[];
  /** File size in bytes */
  size?: number;
}

/**
 * File information for listing
 */
export interface FileInfo {
  path: string;
  filename: string;
  layer: MemoryLayer;
  entryCount: number;
  size: number;
}

/**
 * Result of store operation
 */
export interface StoreResult {
  stored: boolean;
  layer: MemoryLayer;
  file: string;
  importance: number;
  tags: string[];
  entry_id: string;
  /** Reason for rejection (if stored: false) */
  reason?: string;
  /** Suggestion for user (if stored: false or warning present) */
  suggestion?: string;
  /** Warning message (for contradictions, stored: true but flagged) */
  warning?: string;
}

/**
 * Options for store operation
 */
export interface StoreOptions {
  tags?: string[];
  layer?: MemoryLayer;
  scope?: MemoryScope;
  file_hint?: string;
  acquisition_context?: AcquisitionContext;
}

/**
 * Result of retrieve operation
 */
export interface RetrieveResult {
  query_type: string;
  confidence: number;
  files_read: string[];
  context: string;
  token_estimate: number;
  entries_returned: number;
  entries_available: number;
  roi_stats: {
    tokens_saved: number;
    tool_calls_saved: number;
    efficiency_percent: number;
    is_estimated: boolean;
  };
}

/**
 * Options for retrieve operation
 */
export interface RetrieveOptions {
  max_tokens?: number;
  layers?: MemoryLayer[];
  scope?: "all" | "project" | "global";
}

/**
 * A single action taken during consolidation
 */
export interface ConsolidationAction {
  action: "promoted" | "pruned" | "compacted";
  entry_id: string;
  from?: MemoryLayer;
  to?: MemoryLayer;
  importance?: number;
  reason?: string;
}

/**
 * Result of consolidation operation
 */
export interface ConsolidateResult {
  pruned: number;
  promoted: number;
  compacted: number;
  details: ConsolidationAction[];
}

/**
 * Result of update operation
 */
export interface UpdateResult {
  updated: boolean;
  entry_id: string;
  file: string;
  new_importance?: number;
}

/**
 * Options for update operation
 */
export interface UpdateOptions {
  what?: string;
  tags?: string[];
  importance?: number;
}

/**
 * Result of forget operation
 */
export interface ForgetResult {
  forgotten: boolean;
  entry_id: string;
  was_in: string;
}

/**
 * Options for consolidation
 */
export interface ConsolidateOptions {
  scope?: "project" | "global" | "all";
  dry_run?: boolean;
}

/**
 * Result of list operation
 */
export interface ListResult {
  project?: {
    [key in MemoryLayer]?: {
      files: string[];
      entry_count: number;
    };
  };
  global?: {
    [key in MemoryLayer]?: {
      files: string[];
      entry_count: number;
    };
  };
}

/**
 * Context for entry processing
 */
export interface EntryContext {
  existingEntries?: MemoryEntry[];
  recentTags?: string[];
}
