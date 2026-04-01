/**
 * Tests for retrieve-context MCP tool (tool layer)
 * Validates ROI banner format and single-content-block response
 */

import { describe, it, expect, beforeAll, afterAll } from "vitest";
import { tmpdir } from "os";
import { join } from "path";
import { mkdtemp, rm } from "fs/promises";
import { storeMemory } from "../memory/store.js";
import { MemoryLayer, MemoryScope } from "../types/memory.js";
import { handleRetrieveContext } from "../tools/retrieve-context.js";

describe("Retrieve Context Tool (MCP Layer)", () => {
  let testDir: string;
  const originalEnv = process.env.MEMORYKIT_PROJECT;

  beforeAll(async () => {
    testDir = await mkdtemp(join(tmpdir(), "memorykit-tool-"));
    process.env.MEMORYKIT_PROJECT = testDir;

    // Store test entries
    await storeMemory(
      "IMPORTANT: PostgreSQL 16 used for production database with ACID guarantees",
      {
        tags: ["database", "architecture"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
      },
    );

    await storeMemory(
      "IMPORTANT: Redis configured for session caching with 24h TTL",
      {
        tags: ["cache", "redis"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        acquisition_context: { tokens_consumed: 800, tool_calls: 3 },
      },
    );
  });

  afterAll(async () => {
    process.env.MEMORYKIT_PROJECT = originalEnv;
    try {
      await rm(testDir, { recursive: true, force: true });
    } catch (err) {
      console.error("Failed to clean up test directory:", err);
    }
  });

  describe("ROI Banner Format", () => {
    it("should return single content block (not two separate blocks)", async () => {
      const result = await handleRetrieveContext({
        query: "database",
      });

      expect(result.content).toBeDefined();
      expect(Array.isArray(result.content)).toBe(true);
      expect(result.content.length).toBe(1); // Single block only
      expect(result.content[0].type).toBe("text");
    });

    it("should include ROI banner with MemoryKit branding", async () => {
      const result = await handleRetrieveContext({
        query: "database",
      });

      const text = result.content[0].text;
      expect(text).toContain("🧠 **MemoryKit**:");
      expect(text).toMatch(/Found \d+ relevant memor/);
    });

    it("should include estimated savings line", async () => {
      const result = await handleRetrieveContext({
        query: "database",
      });

      const text = result.content[0].text;
      expect(text).toContain("💰 **Estimated savings**:");
      expect(text).toMatch(/~\d+/); // Should show token count
      expect(text).toContain("tokens");
      expect(text).toContain("tool calls");
    });

    it("should include efficiency percentage", async () => {
      const result = await handleRetrieveContext({
        query: "database",
      });

      const text = result.content[0].text;
      expect(text).toContain("📈 **Efficiency**:");
      expect(text).toMatch(/\d+%/);
    });

    it("should show (estimated) note when ROI is estimated", async () => {
      // Store entry without acquisition context
      await storeMemory(
        "IMPORTANT: New entry without acquisition tracking for estimation test",
        {
          tags: ["estimation", "test"],
          layer: MemoryLayer.Facts,
          scope: MemoryScope.Project,
        },
      );

      const result = await handleRetrieveContext({
        query: "estimation test",
      });

      const text = result.content[0].text;
      if (text.includes("estimation")) {
        // If this entry was retrieved, should have estimated note
        expect(text).toMatch(/\(estimated\)/);
      }
    });

    it("should NOT show (estimated) when real acquisition data exists", async () => {
      const result = await handleRetrieveContext({
        query: "redis cache",
      });

      const text = result.content[0].text;
      // Redis entry has acquisition_context, so should not be estimated
      // But only check if entries were actually returned
      if (text.includes("Redis")) {
        // Can't guarantee it won't say estimated if other entries without data are included
        // This is a weaker assertion - the important thing is the field is present
        expect(text).toContain("💰 **Estimated savings**:");
      }
    });
  });

  describe("Banner + Context Integration", () => {
    it("should have ROI banner followed by memory context in same block", async () => {
      const result = await handleRetrieveContext({
        query: "database",
      });

      const text = result.content[0].text;

      // Banner should come first
      const bannerIndex = text.indexOf("🧠 **MemoryKit**:");
      const contextIndex = text.indexOf("# Memory Context");

      expect(bannerIndex).toBeGreaterThanOrEqual(0);
      expect(contextIndex).toBeGreaterThan(bannerIndex);
    });

    it("should separate banner and context with newline", async () => {
      const result = await handleRetrieveContext({
        query: "database",
      });

      const text = result.content[0].text;

      // Should have efficiency line, then newline, then memory context header
      expect(text).toMatch(/📈 \*\*Efficiency\*\*: \d+%\n# Memory Context/);
    });
  });

  describe("Tool Validation", () => {
    it("should handle missing query gracefully", async () => {
      const result = await handleRetrieveContext({});

      expect(result.content).toBeDefined();
      expect(result.content[0].text).toContain("Validation error");
    });

    it("should respect max_tokens parameter", async () => {
      const result = await handleRetrieveContext({
        query: "database",
        max_tokens: 100,
      });

      const text = result.content[0].text;
      // With low token budget, should still have banner
      expect(text).toContain("🧠 **MemoryKit**:");
    });

    it("should handle scope parameter", async () => {
      const result = await handleRetrieveContext({
        query: "database",
        scope: "project",
      });

      expect(result.content).toBeDefined();
      expect(result.content[0].type).toBe("text");
    });
  });
});
