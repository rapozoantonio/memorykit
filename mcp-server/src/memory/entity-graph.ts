/**
 * Tier 2: Entity Extraction and Relationship Graph
 * Lightweight in-memory graph for tracking entities and their relationships
 * Stored as JSON, no database required
 */

import { readFile, writeFile, mkdir } from "fs/promises";
import { existsSync } from "fs";
import { join, dirname } from "path";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
} from "../storage/scope-resolver.js";

// ─── Per-file write lock ────────────────────────────────────────────────────────
// Serializes concurrent write operations on the same file path.
const _graphWriteLocks = new Map<string, Promise<void>>();

async function acquireGraphLock(filePath: string): Promise<() => void> {
  const prev = _graphWriteLocks.get(filePath);
  let release!: () => void;
  const current = new Promise<void>((res) => {
    release = res;
  });
  _graphWriteLocks.set(filePath, current);
  if (prev) await prev;
  return () => {
    release();
    if (_graphWriteLocks.get(filePath) === current) {
      _graphWriteLocks.delete(filePath);
    }
  };
}

async function withGraphLock<T>(
  filePath: string,
  fn: () => Promise<T>,
): Promise<T> {
  const release = await acquireGraphLock(filePath);
  try {
    return await fn();
  } finally {
    release();
  }
}

/**
 * Entity extracted from memory content
 */
export interface Entity {
  /** Entity name (e.g., "UserService", "PostgreSQL") */
  name: string;
  /** Entity type (e.g., "service", "database", "concept") */
  type?: string;
  /** Relationships to other entities */
  relationships?: string[];
}

/**
 * Edge in the entity graph
 */
export interface EntityEdge {
  from: string;
  relation: string;
  to: string;
  entry_id: string; // Which memory entry established this relationship
}

/**
 * Entity graph index
 */
export interface EntityGraph {
  entities: Record<string, { type?: string; entry_ids: string[] }>;
  edges: EntityEdge[];
}

// In-memory cache
const graphCache: Map<string, EntityGraph> = new Map();

/**
 * Get path to entity graph file
 */
function getGraphPath(scope: "project" | "global"): string {
  const root = scope === "project" ? resolveProjectRoot() : resolveGlobalRoot();
  return join(root, "entity-graph.json");
}

/**
 * Load entity graph from disk (or create empty one)
 */
export async function loadEntityGraph(
  scope: "project" | "global",
): Promise<EntityGraph> {
  const cacheKey = scope;
  if (graphCache.has(cacheKey)) {
    return graphCache.get(cacheKey)!;
  }

  const graphPath = getGraphPath(scope);
  if (!existsSync(graphPath)) {
    const emptyGraph: EntityGraph = { entities: {}, edges: [] };
    graphCache.set(cacheKey, emptyGraph);
    return emptyGraph;
  }

  try {
    const content = await readFile(graphPath, "utf-8");
    const graph = JSON.parse(content) as EntityGraph;
    graphCache.set(cacheKey, graph);
    return graph;
  } catch (error) {
    console.error(`Failed to load entity graph from ${graphPath}:`, error);
    const emptyGraph: EntityGraph = { entities: {}, edges: [] };
    graphCache.set(cacheKey, emptyGraph);
    return emptyGraph;
  }
}

/**
 * Save entity graph to disk (with file locking to prevent concurrent write corruption)
 */
export async function saveEntityGraph(
  scope: "project" | "global",
  graph: EntityGraph,
): Promise<void> {
  const graphPath = getGraphPath(scope);

  await withGraphLock(graphPath, async () => {
    // Ensure directory exists
    const dir = dirname(graphPath);
    if (!existsSync(dir)) {
      await mkdir(dir, { recursive: true });
    }

    const content = JSON.stringify(graph, null, 2);
    await writeFile(graphPath, content, "utf-8");
    graphCache.set(scope, graph);
  });
}

/**
 * Add entities from a memory entry
 */
export async function indexEntities(
  entryId: string,
  entities: Entity[],
  scope: "project" | "global",
): Promise<void> {
  const graph = await loadEntityGraph(scope);

  for (const entity of entities) {
    // Register entity
    if (!graph.entities[entity.name]) {
      graph.entities[entity.name] = { type: entity.type, entry_ids: [] };
    }
    if (!graph.entities[entity.name].entry_ids.includes(entryId)) {
      graph.entities[entity.name].entry_ids.push(entryId);
    }
    if (entity.type && !graph.entities[entity.name].type) {
      graph.entities[entity.name].type = entity.type;
    }

    // Parse and add relationships
    if (entity.relationships) {
      for (const rel of entity.relationships) {
        const edge = parseRelationship(entity.name, rel, entryId);
        if (edge) {
          // Check for duplicate
          const exists = graph.edges.some(
            (e) =>
              e.from === edge.from &&
              e.relation === edge.relation &&
              e.to === edge.to,
          );
          if (!exists) {
            graph.edges.push(edge);
          }
        }
      }
    }
  }

  await saveEntityGraph(scope, graph);
}

/**
 * Parse relationship string to edge
 * Formats: "uses PostgreSQL", "depends on Redis", "calls AuthService"
 */
function parseRelationship(
  from: string,
  relationship: string,
  entryId: string,
): EntityEdge | null {
  // Extract relation verb and target
  const patterns = [
    /^(uses?|use)\s+(.+)$/i,
    /^(depends?|depend)\s+on\s+(.+)$/i,
    /^(calls?|call)\s+(.+)$/i,
    /^(extends?|extend)\s+(.+)$/i,
    /^(implements?|implement)\s+(.+)$/i,
    /^(stores?|store)\s+(.+)$/i,
    /^(validates?|validate)\s+(.+)$/i,
    /^(contains?|contain)\s+(.+)$/i,
  ];

  for (const pattern of patterns) {
    const match = relationship.match(pattern);
    if (match) {
      return {
        from,
        relation: match[1].toLowerCase(),
        to: match[2].trim(),
        entry_id: entryId,
      };
    }
  }

  return null;
}

/**
 * Find all entities related to a given entity
 */
export async function findRelatedEntities(
  entityName: string,
  scope: "project" | "global",
): Promise<{ outgoing: EntityEdge[]; incoming: EntityEdge[] }> {
  const graph = await loadEntityGraph(scope);

  return {
    outgoing: graph.edges.filter((e) => e.from === entityName),
    incoming: graph.edges.filter((e) => e.to === entityName),
  };
}

/**
 * Get all entry IDs that mention an entity
 */
export async function getEntriesByEntity(
  entityName: string,
  scope: "project" | "global",
): Promise<string[]> {
  const graph = await loadEntityGraph(scope);
  return graph.entities[entityName]?.entry_ids ?? [];
}

/**
 * Remove entity references for a deleted entry
 */
export async function removeEntryFromGraph(
  entryId: string,
  scope: "project" | "global",
): Promise<void> {
  const graph = await loadEntityGraph(scope);

  // Remove from entity entry_ids
  for (const entity of Object.values(graph.entities)) {
    entity.entry_ids = entity.entry_ids.filter((id) => id !== entryId);
  }

  // Remove edges
  graph.edges = graph.edges.filter((e) => e.entry_id !== entryId);

  await saveEntityGraph(scope, graph);
}
