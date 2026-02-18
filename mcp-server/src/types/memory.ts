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
 * A single memory entry
 */
export interface MemoryEntry {
  /** Unique identifier: e_{timestamp}_{hash} */
  id: string;
  /** The actual memory content */
  content: string;
  /** Amygdala-calculated importance score (0.0-1.0) */
  importance: number;
  /** ISO 8601 creation timestamp */
  created: string;
  /** Categorization tags */
  tags: string[];
  /** Origin of this memory */
  source: MemorySource;
  /** Last modification timestamp (optional) */
  updated?: string;
  /** Custom decay rate override (optional) */
  decay_rate?: number;
  /** Original layer if promoted (optional) */
  promoted_from?: MemoryLayer;
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
}

/**
 * Options for store operation
 */
export interface StoreOptions {
  tags?: string[];
  layer?: MemoryLayer;
  scope?: MemoryScope;
  file_hint?: string;
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
  content?: string;
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
 * Consolidation action detail
 */
export interface ConsolidationAction {
  action: "pruned" | "promoted" | "compacted" | "duplicates_flagged";
  entry_id: string;
  reason?: string;
  from?: MemoryLayer;
  to?: string;
  importance?: number;
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
