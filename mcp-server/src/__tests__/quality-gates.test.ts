/**
 * Tests for Write-Time Quality Gates (M3)
 */

import { describe, it, expect, beforeAll, afterAll } from "vitest";
import { tmpdir } from "os";
import { join } from "path";
import { mkdtemp, rm, mkdir } from "fs/promises";
import { storeMemory } from "../memory/store.js";
import {
  checkImportanceFloor,
  checkDuplicate,
  checkContradiction,
} from "../memory/quality-gate.js";
import { MemoryLayer, MemoryScope } from "../types/memory.js";
import type { MemoryEntry } from "../types/memory.js";

describe("Quality Gates (M3)", () => {
  describe("Gate 1: Importance Floor", () => {
    it("should reject content below threshold", () => {
      const result = checkImportanceFloor(0.08, 0.15);

      expect(result.pass).toBe(false);
      expect(result.reason).toContain("0.08");
      expect(result.reason).toContain("0.15");
      expect(result.suggestion).toBeTruthy();
    });

    it("should accept content above threshold", () => {
      const result = checkImportanceFloor(0.45, 0.15);

      expect(result.pass).toBe(true);
      expect(result.reason).toBeUndefined();
    });

    it("should accept content exactly at threshold", () => {
      const result = checkImportanceFloor(0.15, 0.15);

      expect(result.pass).toBe(true);
    });

    it("should use default threshold of 0.15", () => {
      const result = checkImportanceFloor(0.1);

      expect(result.pass).toBe(false);
      expect(result.reason).toContain("0.15");
    });
  });

  describe("Gate 2: Duplicate Detection", () => {
    const mockExisting: MemoryEntry[] = [
      {
        id: "e_1234567890_abcd",
        title: "PostgreSQL 16 — Primary Database",
        fields: {
          what: "primary database is PostgreSQL 16",
          why: "ACID guarantees",
          tags: "database, architecture, postgresql",
          importance: "0.85",
          created: "2026-02-16",
        },
        what: "primary database is PostgreSQL 16",
        tags: ["database", "architecture", "postgresql"],
        importance: 0.85,
        created: "2026-02-16",
        layer: MemoryLayer.Facts,
        scope: "project",
        filePath: "/test/facts/db.md",
      },
      {
        id: "e_1234567891_efgh",
        title: "Redis — Cache Layer",
        fields: {
          what: "using Redis for caching",
          tags: "cache, redis",
          importance: "0.70",
          created: "2026-02-16",
        },
        what: "using Redis for caching",
        tags: ["cache", "redis"],
        importance: 0.7,
        created: "2026-02-16",
        layer: MemoryLayer.Facts,
        scope: "project",
        filePath: "/test/facts/db.md",
      },
    ];

    it("should detect near-duplicate with high tag overlap and content similarity", () => {
      const newEntry = {
        what: "our primary database is PostgreSQL 16",
        tags: ["database", "postgresql", "architecture"],
      };

      const result = checkDuplicate(newEntry, mockExisting);

      expect(result.pass).toBe(false);
      expect(result.reason).toContain("PostgreSQL 16");
      expect(result.suggestion).toContain("update_memory");
      expect(result.suggestion).toContain("e_1234567890_abcd");
    });

    it("should allow entry with overlapping tags but different content", () => {
      const newEntry = {
        what: "using MongoDB for document storage",
        tags: ["database", "mongodb"],
      };

      const result = checkDuplicate(newEntry, mockExisting);

      expect(result.pass).toBe(true);
    });

    it("should allow entry with similar content but different tags", () => {
      const newEntry = {
        what: "primary database is PostgreSQL 16",
        tags: ["backend", "infrastructure"],
      };

      const result = checkDuplicate(newEntry, mockExisting);

      expect(result.pass).toBe(true); // Tag overlap < 0.6
    });

    it("should allow completely new entry", () => {
      const newEntry = {
        what: "implementing OAuth2 authentication flow",
        tags: ["auth", "security", "oauth"],
      };

      const result = checkDuplicate(newEntry, mockExisting);

      expect(result.pass).toBe(true);
    });

    it("should pass on empty existing entries", () => {
      const newEntry = {
        what: "any content",
        tags: ["test"],
      };

      const result = checkDuplicate(newEntry, []);

      expect(result.pass).toBe(true);
    });
  });

  describe("Gate 3: Contradiction Detection", () => {
    const mockExisting: MemoryEntry[] = [
      {
        id: "e_1234567890_abcd",
        title: "PostgreSQL 16 — Primary Database",
        fields: {
          what: "primary database is PostgreSQL 16",
          tags: "database, architecture",
          importance: "0.85",
          created: "2026-02-16",
        },
        what: "primary database is PostgreSQL 16",
        tags: ["database", "architecture"],
        importance: 0.85,
        created: "2026-02-16",
        layer: MemoryLayer.Facts,
        scope: "project",
        filePath: "/test/facts/db.md",
      },
      {
        id: "e_1234567891_efgh",
        title: "Supabase JWT — Authentication",
        fields: {
          what: "auth uses Supabase JWT tokens",
          tags: "auth, supabase, jwt",
          importance: "0.80",
          created: "2026-02-16",
        },
        what: "auth uses Supabase JWT tokens",
        tags: ["auth", "supabase", "jwt"],
        importance: 0.8,
        created: "2026-02-16",
        layer: MemoryLayer.Facts,
        scope: "project",
        filePath: "/test/facts/auth.md",
      },
    ];

    it("should detect contradiction for same entity with different value", () => {
      const newEntry = {
        what: "primary database is CockroachDB",
        tags: ["database", "architecture"],
      };

      const result = checkContradiction(newEntry, mockExisting);

      expect(result.pass).toBe(true); // Still allows storage
      expect(result.reason).toContain("conflict");
      expect(result.reason).toContain("PostgreSQL 16");
      expect(result.suggestion).toContain("e_1234567890_abcd");
    });

    it("should not flag when entity names differ", () => {
      const newEntry = {
        what: "cache layer uses Redis",
        tags: ["cache", "redis"],
      };

      const result = checkContradiction(newEntry, mockExisting);

      expect(result.pass).toBe(true);
      expect(result.reason).toBeUndefined();
    });

    it("should not flag when tags don't overlap enough", () => {
      const newEntry = {
        what: "primary database is MongoDB",
        tags: ["backend", "nosql"], // Different tags
      };

      const result = checkContradiction(newEntry, mockExisting);

      expect(result.pass).toBe(true);
      expect(result.reason).toBeUndefined(); // Tag overlap < 0.4
    });

    it("should pass on empty existing entries", () => {
      const newEntry = {
        what: "any content",
        tags: ["test"],
      };

      const result = checkContradiction(newEntry, []);

      expect(result.pass).toBe(true);
      expect(result.reason).toBeUndefined();
    });
  });

  describe("Integration: Quality Gates in Store", () => {
    let testDir: string;
    const originalEnv = process.env.MEMORYKIT_PROJECT;

    beforeAll(async () => {
      // Create temp directory
      testDir = await mkdtemp(join(tmpdir(), "memorykit-m3-"));

      // Override project path
      process.env.MEMORYKIT_PROJECT = testDir;

      // Create layer directories
      const memoryKitPath = join(testDir, ".memorykit");
      await mkdir(join(memoryKitPath, "working"), { recursive: true });
      await mkdir(join(memoryKitPath, "facts"), { recursive: true });
      await mkdir(join(memoryKitPath, "episodes"), { recursive: true });
      await mkdir(join(memoryKitPath, "procedures"), { recursive: true });
    });

    afterAll(async () => {
      // Cleanup
      process.env.MEMORYKIT_PROJECT = originalEnv;

      try {
        await rm(testDir, { recursive: true, force: true });
      } catch (err) {
        console.error("Failed to clean up test directory:", err);
      }
    });

    it("should reject low-importance content", async () => {
      const trivialContent = "ok sounds good";

      const result = await storeMemory(trivialContent, {
        tags: ["conversation"],
        layer: MemoryLayer.Working,
        scope: MemoryScope.Project,
      });

      expect(result.stored).toBe(false);
      expect(result.reason).toBeTruthy();
      expect(result.reason).toContain("threshold");
      expect(result.suggestion).toBeTruthy();
    });

    it("should store high-importance content", async () => {
      const importantContent =
        "We decided to use PostgreSQL 16 because of ACID compliance and pgvector support.";

      const result = await storeMemory(importantContent, {
        tags: ["database", "decision"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
      });

      expect(result.stored).toBe(true);
      expect(result.entry_id).toBeTruthy();
      expect(result.importance).toBeGreaterThan(0.15);
    });

    it("should reject duplicate entry", async () => {
      const content1 = "Using TypeScript for type safety and better tooling.";
      const content2 = "We're using TypeScript for type safety and tooling.";

      // Store first entry
      const result1 = await storeMemory(content1, {
        tags: ["typescript", "tooling"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        file_hint: "tech-stack.md",
      });

      expect(result1.stored).toBe(true);

      // Try to store duplicate
      const result2 = await storeMemory(content2, {
        tags: ["typescript", "tooling"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        file_hint: "tech-stack.md",
      });

      expect(result2.stored).toBe(false);
      expect(result2.reason).toContain("duplicate");
      expect(result2.suggestion).toContain("update_memory");
      expect(result2.suggestion).toContain(result1.entry_id);
    });

    it("should allow distinct entries with overlapping tags", async () => {
      const content1 =
        "Using PostgreSQL for relational data storage with ACID guarantees.";
      const content2 =
        "Using MongoDB for flexible document storage with horizontal scaling.";

      const result1 = await storeMemory(content1, {
        tags: ["database", "postgresql"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        file_hint: "databases.md",
      });

      const result2 = await storeMemory(content2, {
        tags: ["database", "mongodb"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        file_hint: "databases.md",
      });

      expect(result1.stored).toBe(true);
      expect(result2.stored).toBe(true); // Different enough content
    });

    it("should propagate warnings from quality gates", async () => {
      // This test verifies that warnings (like contradictions) are returned in StoreResult
      // We test contradiction detection thoroughly at the unit level above
      // Here we just verify the integration: warnings field exists and propagates

      const content =
        "Authentication uses Supabase JWT tokens validated by custom middleware.";

      const result = await storeMemory(content, {
        tags: ["auth", "jwt"],
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
      });

      expect(result.stored).toBe(true);
      expect(result.entry_id).toBeTruthy();

      // Warning field should exist (may be undefined, that's fine)
      expect(result).toHaveProperty("warning");
      expect(result).toHaveProperty("suggestion");
    });
  });
});
