/**
 * Integration tests for M1 + M2: MML Pipeline
 * Tests: Store prose → Normalize → Write → Read → Parse
 */

import { describe, it, expect, beforeAll, afterAll } from "vitest";
import { tmpdir } from "os";
import { join } from "path";
import { mkdtemp, rm, mkdir } from "fs/promises";
import { storeMemory } from "../memory/store.js";
import { readMemoryFile } from "../storage/file-manager.js";
import { resolveFilePath } from "../storage/scope-resolver.js";
import { MemoryLayer, MemoryScope } from "../types/memory.js";

describe("MML Pipeline Integration (M1+M2)", () => {
  let testDir: string;
  const originalEnv = process.env.MEMORYKIT_PROJECT;

  beforeAll(async () => {
    // Create temp directory
    testDir = await mkdtemp(join(tmpdir(), "memorykit-m1m2-"));

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

  it("should store prose decision, normalize to MML, and parse back", async () => {
    const prose =
      "We decided to use PostgreSQL 16 as our primary database because of ACID guarantees and pgvector support. We rejected MongoDB due to lack of multi-document transactions.";

    // Store
    const storeResult = await storeMemory(prose, {
      tags: ["database", "architecture"],
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
    });

    expect(storeResult.stored).toBe(true);
    expect(storeResult.layer).toBe(MemoryLayer.Facts);

    // Read back
    const filePath = resolveFilePath(
      MemoryScope.Project,
      MemoryLayer.Facts,
      storeResult.file,
    );
    const entries = await readMemoryFile(filePath);

    // Verify entry was stored and parsed correctly
    expect(entries.length).toBeGreaterThan(0);

    const entry = entries[entries.length - 1]; // Get last entry
    expect(entry.title).toContain("PostgreSQL");
    expect(entry.what.toLowerCase()).toContain("postgresql");
    expect(entry.why).toBeTruthy();
    expect(entry.why?.toLowerCase()).toContain("acid");
    expect(entry.rejected).toBeTruthy();
    expect(entry.rejected?.toLowerCase()).toContain("mongodb");
    expect(entry.tags).toContain("database");
    expect(entry.layer).toBe(MemoryLayer.Facts);
    expect(entry.scope).toBe("project");
  });

  it("should store bug report prose, normalize to MML episode format", async () => {
    const prose =
      "Found a race condition in OrderService.ProcessPayment() that causes duplicate charges. Fixed by adding IsolationLevel.Serializable to the transaction.";

    const storeResult = await storeMemory(prose, {
      tags: ["bug", "payment", "race-condition"],
      layer: MemoryLayer.Episodes,
      scope: MemoryScope.Project,
    });

    expect(storeResult.stored).toBe(true);
    expect(storeResult.layer).toBe(MemoryLayer.Episodes);

    // Read back
    const filePath = resolveFilePath(
      MemoryScope.Project,
      MemoryLayer.Episodes,
      storeResult.file,
    );
    const entries = await readMemoryFile(filePath);

    const entry = entries[entries.length - 1];
    expect(entry.title.toLowerCase()).toContain("race condition");
    expect(entry.what.toLowerCase()).toContain("race condition");
    expect(entry.symptom).toBeTruthy();
    expect(entry.symptom?.toLowerCase()).toContain("duplicate");
    expect(entry.fix).toBeTruthy();
    expect(entry.fix?.toLowerCase()).toContain("serializable");
    expect(entry.file).toBe("OrderService.cs");
  });

  it("should store rule prose, normalize to MML procedure format", async () => {
    const prose =
      "Always validate API input with FluentValidation. Never use DataAnnotations for complex validation rules.";

    const storeResult = await storeMemory(prose, {
      tags: ["api", "validation", "pattern"],
      layer: MemoryLayer.Procedures,
      scope: MemoryScope.Project,
    });

    expect(storeResult.stored).toBe(true);
    expect(storeResult.layer).toBe(MemoryLayer.Procedures);

    // Read back
    const filePath = resolveFilePath(
      MemoryScope.Project,
      MemoryLayer.Procedures,
      storeResult.file,
    );
    const entries = await readMemoryFile(filePath);

    const entry = entries[entries.length - 1];
    expect(entry.what.toLowerCase()).toContain("validate");
    expect(entry.do).toBeTruthy();
    expect(entry.do?.toLowerCase()).toContain("fluentvalidation");
    expect(entry.dont).toBeTruthy();
    expect(entry.dont?.toLowerCase()).toContain("dataannotations");
  });

  it("should handle pre-structured MML input", async () => {
    const mml = `### Redis — Cache Layer
- **what**: using Redis for caching
- **why**: fast in-memory access, supports pub/sub
- **tags**: cache, redis, architecture
- **importance**: 0.70
- **created**: 2026-02-16`;

    const storeResult = await storeMemory(mml, {
      tags: ["cache", "redis"],
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
    });

    expect(storeResult.stored).toBe(true);

    // Read back
    const filePath = resolveFilePath(
      MemoryScope.Project,
      MemoryLayer.Facts,
      storeResult.file,
    );
    const entries = await readMemoryFile(filePath);

    const entry = entries[entries.length - 1];
    expect(entry.title).toBe("Redis — Cache Layer");
    expect(entry.what).toBe("using Redis for caching");
    expect(entry.why).toBe("fast in-memory access, supports pub/sub");
  });

  it("should store multiple entries in same file correctly", async () => {
    const prose1 = "Using TypeScript for type safety.";
    const prose2 = "Using ESLint for code quality.";
    const prose3 = "Using Prettier for formatting.";

    await storeMemory(prose1, {
      tags: ["typescript", "tooling"],
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
      file_hint: "tooling.md",
    });

    await storeMemory(prose2, {
      tags: ["eslint", "tooling"],
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
      file_hint: "tooling.md",
    });

    await storeMemory(prose3, {
      tags: ["prettier", "tooling"],
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
      file_hint: "tooling.md",
    });

    // Read back
    const filePath = resolveFilePath(
      MemoryScope.Project,
      MemoryLayer.Facts,
      "tooling.md",
    );
    const entries = await readMemoryFile(filePath);

    expect(entries.length).toBe(3);
    expect(entries[0].what.toLowerCase()).toContain("typescript");
    expect(entries[1].what.toLowerCase()).toContain("eslint");
    expect(entries[2].what.toLowerCase()).toContain("prettier");

    // Verify no content bleeding
    expect(entries[0].what).not.toContain("ESLint");
    expect(entries[1].what).not.toContain("Prettier");
    expect(entries[2].what).not.toContain("TypeScript");
  });

  it("should produce token-efficient MML output", async () => {
    const verboseProse =
      "So basically, after a lot of discussion with the team, we kind of decided that we should probably go ahead and use PostgreSQL for our database. The main reason for this is that it has really good ACID compliance and stuff, and also it's pretty mature with a big ecosystem. We also thought about MongoDB but we rejected it because it doesn't really have proper multi-document transactions.";

    const storeResult = await storeMemory(verboseProse, {
      tags: ["database"],
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
    });

    // Read back
    const filePath = resolveFilePath(
      MemoryScope.Project,
      MemoryLayer.Facts,
      storeResult.file,
    );
    const entries = await readMemoryFile(filePath);
    const entry = entries[entries.length - 1];

    // Calculate approximate token counts
    const proseTokens = verboseProse.length / 4;
    const mmlContent =
      entry.what + " " + (entry.why || "") + " " + (entry.rejected || "");
    const mmlTokens = mmlContent.length / 4;

    expect(mmlTokens).toBeLessThan(proseTokens * 0.7); // Should use ≤70% tokens
  });

  it("should preserve all required MML fields through round-trip", async () => {
    const prose = "Using Vite for build tooling because it's fast.";

    const storeResult = await storeMemory(prose, {
      tags: ["build", "tooling"],
      layer: MemoryLayer.Facts,
      scope: MemoryScope.Project,
    });

    // Read back
    const filePath = resolveFilePath(
      MemoryScope.Project,
      MemoryLayer.Facts,
      storeResult.file,
    );
    const entries = await readMemoryFile(filePath);
    const entry = entries[entries.length - 1];

    // Verify all required fields
    expect(entry.id).toBeTruthy();
    expect(entry.id).toMatch(/^e_\d+_[a-f0-9]{4}$/);
    expect(entry.title).toBeTruthy();
    expect(entry.what).toBeTruthy();
    expect(entry.tags).toBeInstanceOf(Array);
    expect(entry.tags.length).toBeGreaterThan(0);
    expect(entry.importance).toBeGreaterThan(0);
    expect(entry.importance).toBeLessThanOrEqual(1);
    expect(entry.created).toBeTruthy();
    expect(entry.created).toMatch(/^\d{4}-\d{2}-\d{2}/);
    expect(entry.layer).toBe(MemoryLayer.Facts);
    expect(entry.scope).toBe("project");
    expect(entry.filePath).toBeTruthy();
  });
});
