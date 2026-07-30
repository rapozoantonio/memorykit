/**
 * Embedding cache - Persists computed embeddings outside the human-readable
 * .md files (which intentionally never serialize the `embedding` field, see
 * MemoryEntry.embedding in types/memory.ts) so retrieval doesn't re-run the
 * local transformer model for every entry on every call.
 *
 * One cache file per scope root: <root>/.embeddings-cache.json
 * Keyed by entry id, invalidated by a hash of the embedded text so edits
 * (update_memory, consolidation compaction) transparently recompute.
 */

import { readFile, writeFile, mkdir } from "fs/promises";
import { existsSync } from "fs";
import { join } from "path";
import { createHash } from "crypto";

interface CachedVector {
  hash: string;
  vector: number[];
}

type CacheFile = Record<string, CachedVector>;

// Caches the *promise* (not the resolved object) per root, keyed synchronously
// before any await. This dedupes concurrent first-callers for the same root —
// e.g. retrieve.ts backfills embeddings for multiple entries via Promise.all —
// so they all resolve to the same shared CacheFile object instead of each
// parsing their own independent copy and clobbering each other's writes.
const _cachePromises = new Map<string, Promise<CacheFile>>();
// Serializes the actual disk writes per root. Because callers share the same
// CacheFile object (via _cachePromises above), every queued write serializes
// the full current state of that shared object — so even if two writes race,
// the one that lands last still contains both callers' entries.
const _writeQueue = new Map<string, Promise<void>>();

function cachePath(root: string): string {
  return join(root, ".embeddings-cache.json");
}

function hashText(text: string): string {
  return createHash("sha256").update(text).digest("hex").substring(0, 16);
}

function loadCache(root: string): Promise<CacheFile> {
  const existing = _cachePromises.get(root);
  if (existing) return existing;

  const promise = (async (): Promise<CacheFile> => {
    const path = cachePath(root);
    if (existsSync(path)) {
      try {
        return JSON.parse(await readFile(path, "utf-8"));
      } catch {
        return {};
      }
    }
    return {};
  })();

  _cachePromises.set(root, promise);
  return promise;
}

/**
 * Look up a cached embedding for an entry. Returns null on miss or if the
 * embedded text has changed since it was cached.
 */
export async function getCachedEmbedding(
  root: string,
  entryId: string,
  text: string,
): Promise<number[] | null> {
  const cache = await loadCache(root);
  const hit = cache[entryId];
  if (hit && hit.hash === hashText(text)) {
    return hit.vector;
  }
  return null;
}

/**
 * Store an embedding for an entry, persisted to the scope's cache file.
 */
export async function setCachedEmbedding(
  root: string,
  entryId: string,
  text: string,
  vector: number[],
): Promise<void> {
  const cache = await loadCache(root);
  cache[entryId] = { hash: hashText(text), vector };

  const path = cachePath(root);
  const prev = _writeQueue.get(root) ?? Promise.resolve();
  // store.ts calls this before appendEntry() has created the scope root
  // (embedding generation runs ahead of the actual file write), so the
  // directory may not exist yet on a project's very first store — ensure
  // it does rather than silently failing the cache write.
  const next = prev
    .catch(() => {})
    .then(() => mkdir(root, { recursive: true }))
    .then(() => writeFile(path, JSON.stringify(cache), "utf-8"));
  _writeQueue.set(root, next);
  await next;
}
