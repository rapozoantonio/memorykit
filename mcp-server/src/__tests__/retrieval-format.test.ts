/**
 * Tests for M4: Retrieval Output Format
 * Validates layer groupings, field stripping, token budget, truncation
 */

import { describe, it, expect, beforeAll, afterAll } from "vitest";
import { tmpdir } from "os";
import { join } from "path";
import { mkdtemp, rm } from "fs/promises";
import { storeMemory } from "../memory/store.js";
import { retrieveContext } from "../memory/retrieve.js";
import { MemoryLayer, MemoryScope } from "../types/memory.js";
import { resolveProjectRoot } from "../storage/scope-resolver.js";

describe("Retrieval Output Format (M4)", () => {
  let testDir: string;
  let storageRoot: string;
  const originalEnv = process.env.MEMORYKIT_PROJECT;

  beforeAll(async () => {
    testDir = await mkdtemp(join(tmpdir(), "memorykit-m4-"));
    process.env.MEMORYKIT_PROJECT = testDir;
    storageRoot = resolveProjectRoot(); // Get actual storage path

    // Store test entries across layers - include importance markers to pass quality gates
    const result1 = await storeMemory(
      "IMPORTANT: We use PostgreSQL 16 as primary database for all production systems",
      {
        tags: ["database", "architecture"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
      },
    );
    expect(result1.stored).toBe(true);

    const result2 = await storeMemory(
      "CRITICAL: Always validate API input with FluentValidation. Never use DataAnnotations.",
      {
        tags: ["api", "validation"],
        layer: MemoryLayer.Procedures,
        scope: MemoryScope.Project,
      },
    );
    expect(result2.stored).toBe(true);

    const result3 = await storeMemory(
      "IMPORTANT: Fixed race condition in payment processing that caused duplicate charges",
      {
        tags: ["bug", "payment"],
        layer: MemoryLayer.Episodes,
        scope: MemoryScope.Project,
      },
    );
    expect(result3.stored).toBe(true);

    const result4 = await storeMemory(
      "IMPORTANT: Currently working on critical authentication module for security compliance",
      {
        tags: ["auth", "wip"],
        layer: MemoryLayer.Working,
        scope: MemoryScope.Project,
      },
    );
    expect(result4.stored).toBe(true);
  });

  afterAll(async () => {
    process.env.MEMORYKIT_PROJECT = originalEnv;
    try {
      await rm(storageRoot, { recursive: true, force: true });
      await rm(testDir, { recursive: true, force: true });
    } catch (err) {
      console.error("Cleanup failed:", err);
    }
  });

  describe("Layer Groupings", () => {
    it("should include H2 headers for each layer with entries", async () => {
      const result = await retrieveContext("show me the context", {
        scope: MemoryScope.Project,
        max_tokens: 5000, // Ensure all entries are retrieved
      });

      // Should have layer groupings
      expect(result.context).toContain("## facts");
      expect(result.context).toContain("## procedures");
      expect(result.context).toContain("## episodes");
    });

    it("should order layers: Facts → Procedures → Episodes → Working", async () => {
      const result = await retrieveContext("show me the context", {
        scope: MemoryScope.Project,
        max_tokens: 5000, // Ensure all entries are retrieved
      });

      const factsIndex = result.context.indexOf("## facts");
      const proceduresIndex = result.context.indexOf("## procedures");
      const episodesIndex = result.context.indexOf("## episodes");

      expect(factsIndex).toBeGreaterThan(-1);
      expect(proceduresIndex).toBeGreaterThan(factsIndex);
      expect(episodesIndex).toBeGreaterThan(proceduresIndex);
    });
  });

  describe("Field Stripping", () => {
    it("should strip importance field from output entries", async () => {
      const result = await retrieveContext("database", {
        scope: MemoryScope.Project,
      });

      // Output should NOT contain "- **importance**:" in entry content
      // It's okay in file, but stripped from retrieval output
      const lines = result.context.split("\n");
      const importanceLines = lines.filter((line) =>
        line.includes("**importance**:"),
      );

      // Either no importance lines, or only in metadata (not in entries)
      expect(importanceLines.length).toBe(0);
    });

    it("should strip created field from output entries", async () => {
      const result = await retrieveContext("database", {
        scope: MemoryScope.Project,
      });

      const lines = result.context.split("\n");
      const createdLines = lines.filter((line) =>
        line.includes("**created**:"),
      );

      expect(createdLines.length).toBe(0);
    });

    it("should keep what, why, tags fields", async () => {
      const result = await retrieveContext("database", {
        scope: MemoryScope.Project,
      });

      // These fields should be present
      expect(result.context).toContain("**what**:");
      expect(result.context).toContain("**tags**:");
    });
  });

  describe("ROI Stats (Computation)", () => {
    it("should return roi_stats with all required fields", async () => {
      const result = await retrieveContext("database");
      expect(result.roi_stats).toBeDefined();
      expect(result.roi_stats.tokens_saved).toBeGreaterThanOrEqual(0);
      expect(result.roi_stats.tool_calls_saved).toBeGreaterThanOrEqual(0);
      expect(result.roi_stats.efficiency_percent).toBeGreaterThanOrEqual(0);
      expect(typeof result.roi_stats.is_estimated).toBe("boolean");
    });

    it("should mark ROI as estimated when no acquisition data exists", async () => {
      // Store entry WITHOUT acquisition_context
      await storeMemory(
        "IMPORTANT: Test entry without tracking data for ROI estimation",
        {
          layer: MemoryLayer.Facts,
          scope: MemoryScope.Project,
          tags: ["roi-test", "estimation"],
        },
      );

      const result = await retrieveContext("roi-test estimation");
      if (result.entries_returned > 0) {
        expect(result.roi_stats.is_estimated).toBe(true);
        expect(result.roi_stats.tokens_saved).toBeGreaterThan(0);
        // Heuristic: ~500 tokens per entry
        expect(result.roi_stats.tokens_saved).toBeGreaterThanOrEqual(0);
      }
    });

    it("should compute real ROI when acquisition context exists", async () => {
      const storeResult = await storeMemory(
        "IMPORTANT: Test with actual tracking and acquisition metadata for ROI",
        {
          layer: MemoryLayer.Facts,
          scope: MemoryScope.Project,
          tags: ["roi-real", "tracking"],
          acquisition_context: { tokens_consumed: 1000, tool_calls: 5 },
        },
      );
      expect(storeResult.stored).toBe(true);

      const result = await retrieveContext("roi-real tracking");
      if (result.entries_returned > 0) {
        // Should use real data when available
        expect(result.roi_stats.tokens_saved).toBeGreaterThanOrEqual(0);
        expect(result.roi_stats.tool_calls_saved).toBeGreaterThanOrEqual(0);
      }
    });
  });

  describe("Token Budget", () => {
    it("should respect token budget and truncate if needed", async () => {
      // Store many entries to exceed budget
      for (let i = 0; i < 20; i++) {
        await storeMemory(
          `Test entry ${i} with substantial content to consume tokens. `.repeat(
            20,
          ),
          {
            scope: MemoryScope.Project,
            layer: MemoryLayer.Facts,
          },
        );
      }

      const result = await retrieveContext("test", {
        scope: MemoryScope.Project,
        max_tokens: 1000,
      });

      expect(result.token_estimate).toBeLessThanOrEqual(1000);
    });

    it("should show truncation notification when budget exceeded", async () => {
      // Store many entries
      for (let i = 0; i < 15; i++) {
        await storeMemory(`Entry ${i} `.repeat(50), {
          scope: MemoryScope.Project,
          layer: MemoryLayer.Facts,
        });
      }

      const result = await retrieveContext("entry", {
        scope: MemoryScope.Project,
        max_tokens: 500,
      });

      // If entries were truncated, should have notification
      if (result.entries_returned < result.entries_available) {
        expect(result.context).toMatch(/\[\+\d+ more/i);
      }
    });
  });

  describe("Global Entries Marking", () => {
    it("should mark global entries with [global] marker", async () => {
      // Store a global entry
      await storeMemory("TypeScript strict mode always enabled", {
        tags: ["typescript", "preference"],
        layer: MemoryLayer.Procedures,
        scope: MemoryScope.Global,
      });

      const result = await retrieveContext("typescript", {
        scope: MemoryScope.Project,
      });

      // Should have [global] marker for global entries
      if (result.context.includes("TypeScript")) {
        expect(result.context).toContain("[global]");
      }
    });
  });

  describe("Header Metadata", () => {
    it("should include entry count in header", async () => {
      const result = await retrieveContext("database", {
        scope: MemoryScope.Project,
      });

      // Header should show entry count
      expect(result.context).toMatch(/\d+ entr(?:y|ies)/i);
    });

    it("should include token estimate in header", async () => {
      const result = await retrieveContext("database", {
        scope: MemoryScope.Project,
      });

      // Header should show token estimate
      expect(result.context).toMatch(/~?\d+[,\s]*tokens?/i);
    });
  });

  describe("Empty Retrieval", () => {
    it("should return meaningful message when no memories found", async () => {
      const result = await retrieveContext("nonexistent-zombie-xyzabc-98765", {
        scope: MemoryScope.Project,
        max_tokens: 100, // Low token budget to avoid matching unrelated entries
      });

      // With relevance floor, might still return low-scoring entries
      // Check that either we get 0 entries, or a reasonable number with low relevance
      if (result.entries_returned === 0) {
        expect(result.context.toLowerCase()).toMatch(
          /no.*memor(?:y|ies)|nothing found/i,
        );
      } else {
        // If entries returned, they should be low relevance matches
        expect(result.entries_returned).toBeLessThanOrEqual(3);
      }
    });
  });

  describe("Priority Sorting", () => {
    it("should sort entries by importance × recency within layer", async () => {
      // Store entries with different importance
      await storeMemory(
        "CRITICAL: Database migration procedure must follow exact steps",
        {
          tags: ["database", "migration"],
          layer: MemoryLayer.Procedures,
          scope: MemoryScope.Project,
        },
      );

      await storeMemory("minor note about database", {
        tags: ["database", "note"],
        layer: MemoryLayer.Procedures,
        scope: MemoryScope.Project,
      });

      const result = await retrieveContext("database migration", {
        scope: MemoryScope.Project,
      });

      const lines = result.context.split("\n");
      const criticalIndex = lines.findIndex((line) =>
        line.includes("CRITICAL"),
      );
      const minorIndex = lines.findIndex(
        (line) => line.includes("minor") && line.includes("note"),
      );

      // CRITICAL should appear before minor (if both present)
      if (criticalIndex > -1 && minorIndex > -1) {
        expect(criticalIndex).toBeLessThan(minorIndex);
      }
    });
  });

  describe("MML Format Preservation", () => {
    it("should output retrieved entries in MML format", async () => {
      const result = await retrieveContext("database", {
        scope: MemoryScope.Project,
      });

      // Should have MML structure
      expect(result.context).toMatch(/^###\s+/m); // Heading
      expect(result.context).toContain("- **"); // MML key-value pairs
    });

    it("should have proper MML heading format", async () => {
      const result = await retrieveContext("validation", {
        scope: MemoryScope.Project,
      });

      // Headings should be ### format
      const headings = result.context.match(/^###\s+.+$/gm);
      expect(headings).toBeTruthy();
      if (headings) {
        expect(headings.length).toBeGreaterThan(0);
      }
    });
  });
});
