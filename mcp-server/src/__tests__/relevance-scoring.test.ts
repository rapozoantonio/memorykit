/**
 * Tests for query-relevance scoring in retrieval
 * Validates that relevant entries rank higher than irrelevant high-importance entries
 */

import { describe, it, expect, beforeAll, afterAll } from "vitest";
import { tmpdir } from "os";
import { join } from "path";
import { mkdtemp, rm } from "fs/promises";
import { storeMemory } from "../memory/store.js";
import { retrieveContext } from "../memory/retrieve.js";
import { MemoryLayer, MemoryScope } from "../types/memory.js";

describe("Query-Relevance Scoring", () => {
  let testDir: string;
  const originalEnv = process.env.MEMORYKIT_PROJECT;

  beforeAll(async () => {
    testDir = await mkdtemp(join(tmpdir(), "memorykit-relevance-"));
    process.env.MEMORYKIT_PROJECT = testDir;
  });

  afterAll(async () => {
    process.env.MEMORYKIT_PROJECT = originalEnv;
    if (testDir) {
      await rm(testDir, { recursive: true, force: true });
    }
  });

  it("should rank relevant low-importance entries above irrelevant high-importance entries", async () => {
    // Store high-importance but irrelevant entry
    await storeMemory(
      "IMPORTANT: We use PostgreSQL for database storage with connection pooling",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        acquisition_context: { tokens_consumed: 100, tool_calls: 1 },
      },
    );

    // Store lower-importance but relevant entry
    await storeMemory(
      "IMPORTANT: Authentication uses JWT middleware in src/middleware/auth.ts",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        acquisition_context: { tokens_consumed: 100, tool_calls: 1 },
      },
    );

    // Query for authentication
    const result = await retrieveContext("how do we handle authentication?");

    // Parse context to extract entry order
    const lines = result.context.split("\n");
    const entryHeadings = lines.filter((line) => line.startsWith("### "));

    // First meaningful entry should be about authentication
    const firstEntry = entryHeadings[0];
    expect(firstEntry).toMatch(/authentication|jwt|auth/i);
  });

  it("should still surface high-importance entries when query matches", async () => {
    // Store high-importance matching entry
    await storeMemory(
      "CRITICAL: Authentication must use HTTPS only in production",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        acquisition_context: { tokens_consumed: 150, tool_calls: 2 },
      },
    );

    const result = await retrieveContext(
      "authentication security requirements",
    );

    expect(result.entries_returned).toBeGreaterThan(0);
    expect(result.context).toMatch(/authentication.*https/i);
  });

  it("should handle queries with no matching entries gracefully", async () => {
    // Store unrelated entries
    await storeMemory("IMPORTANT: We use React for frontend development", {
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
      acquisition_context: { tokens_consumed: 50, tool_calls: 1 },
    });

    const result = await retrieveContext("blockchain cryptocurrency");

    // Should still return entries (with low relevance floor of 0.1)
    expect(result.entries_returned).toBeGreaterThan(0);
  });

  it("should give partial credit for substring matches", async () => {
    // Store entry with "authentication"
    await storeMemory("IMPORTANT: Authentication flow uses OAuth2 protocol", {
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
      acquisition_context: { tokens_consumed: 100, tool_calls: 1 },
    });

    // Query with "auth" (substring of "authentication")
    const result = await retrieveContext("how does auth work?");

    expect(result.entries_returned).toBeGreaterThan(0);
    expect(result.context).toMatch(/authentication|oauth/i);
  });

  it("should filter stop words from relevance calculation", async () => {
    // Store entry
    await storeMemory("IMPORTANT: Repository pattern is used for data access", {
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
      acquisition_context: { tokens_consumed: 80, tool_calls: 1 },
    });

    // Query with many stop words
    const result = await retrieveContext(
      "what is the pattern we use for data?",
    );

    // Should match on "pattern" and "data", ignoring "what", "is", "the", "we", "use", "for"
    expect(result.context).toMatch(/repository.*pattern/i);
  });
});
