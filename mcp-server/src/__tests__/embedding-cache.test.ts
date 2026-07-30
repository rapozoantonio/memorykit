/**
 * Tests for the embedding cache (persists embeddings outside the .md files)
 */

import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { mkdtemp, rm, readFile } from "fs/promises";
import { tmpdir } from "os";
import { join } from "path";
import { getCachedEmbedding, setCachedEmbedding } from "../memory/embedding-cache.js";

describe("Embedding Cache", () => {
  let root: string;

  beforeEach(async () => {
    root = await mkdtemp(join(tmpdir(), "memorykit-embedding-cache-"));
  });

  afterEach(async () => {
    await rm(root, { recursive: true, force: true });
  });

  it("should return null on a cold cache", async () => {
    const result = await getCachedEmbedding(root, "e_1", "some text");
    expect(result).toBeNull();
  });

  it("should return the cached vector when the text is unchanged", async () => {
    await setCachedEmbedding(root, "e_1", "some text", [0.1, 0.2, 0.3]);

    const result = await getCachedEmbedding(root, "e_1", "some text");
    expect(result).toEqual([0.1, 0.2, 0.3]);
  });

  it("should miss when the text has changed since caching", async () => {
    await setCachedEmbedding(root, "e_1", "original text", [0.1, 0.2, 0.3]);

    const result = await getCachedEmbedding(root, "e_1", "edited text");
    expect(result).toBeNull();
  });

  it("should not lose entries when caching concurrently for the same root", async () => {
    // Regression test: concurrent setCachedEmbedding calls for different
    // entries under the same root must not clobber each other's writes.
    // This mirrors retrieve.ts's Promise.all over multiple entries missing
    // a cached embedding in the same scope.
    await Promise.all(
      Array.from({ length: 10 }, (_, i) =>
        setCachedEmbedding(root, `e_${i}`, `text ${i}`, [i, i + 1]),
      ),
    );

    for (let i = 0; i < 10; i++) {
      const result = await getCachedEmbedding(root, `e_${i}`, `text ${i}`);
      expect(result).toEqual([i, i + 1]);
    }

    // Verify all 10 entries actually made it to disk, not just in-memory.
    const raw = await readFile(join(root, ".embeddings-cache.json"), "utf-8");
    const parsed = JSON.parse(raw);
    expect(Object.keys(parsed)).toHaveLength(10);
  });
});
