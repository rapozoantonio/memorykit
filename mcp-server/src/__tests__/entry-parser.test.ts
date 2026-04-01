/**
 * Tests for MML entry parser (M1)
 */

import { describe, it, expect } from "vitest";
import {
  parseMMLLine,
  parseEntry,
  serializeEntry,
  parseEntries,
  serializeEntries,
  extractHeader,
  generateEntryId,
} from "../storage/entry-parser.js";
import { MemoryLayer } from "../types/memory.js";
import type { MemoryEntry } from "../types/memory.js";

describe("MML Entry Parser (M1)", () => {
  describe("parseMMLLine", () => {
    it("should parse well-formed MML line", () => {
      const result = parseMMLLine(
        "- **what**: primary database is PostgreSQL 16",
      );
      expect(result).toEqual(["what", "primary database is PostgreSQL 16"]);
    });

    it("should handle colons in values (timestamps)", () => {
      const result = parseMMLLine("- **created**: 2026-02-16T10:30:00Z");
      expect(result).toEqual(["created", "2026-02-16T10:30:00Z"]);
    });

    it("should handle colons in values (ISO dates)", () => {
      const result = parseMMLLine("- **created**: 2026-02-16");
      expect(result).toEqual(["created", "2026-02-16"]);
    });

    it("should handle commas in values", () => {
      const result = parseMMLLine(
        "- **why**: ACID guarantees, mature ecosystem, pgvector for embeddings",
      );
      expect(result).toEqual([
        "why",
        "ACID guarantees, mature ecosystem, pgvector for embeddings",
      ]);
    });

    it("should return null for non-MML lines", () => {
      expect(parseMMLLine("Just regular text")).toBeNull();
      expect(parseMMLLine("- regular list item")).toBeNull();
      expect(parseMMLLine("**bold** but not MML")).toBeNull();
    });

    it("should handle hyphenated keys", () => {
      const result = parseMMLLine(
        "- **root-cause**: connection pool exhaustion",
      );
      expect(result).toEqual(["root-cause", "connection pool exhaustion"]);
    });
  });

  describe("parseEntry", () => {
    const baseParams: [MemoryLayer, "project" | "global", string] = [
      MemoryLayer.Facts,
      "project",
      "/test/facts/database.md",
    ];

    it("should parse well-formed MML entry", () => {
      const mml = `### PostgreSQL 16 — Primary Database
- **what**: primary database is PostgreSQL 16
- **why**: ACID guarantees, mature ecosystem, pgvector for embeddings
- **rejected**: MongoDB (no multi-doc txns), DynamoDB (no txns)
- **constraint**: financial domain requires strict consistency
- **tags**: database, architecture, postgresql
- **importance**: 0.85
- **created**: 2026-02-16`;

      const entry = parseEntry(mml, ...baseParams);

      expect(entry).not.toBeNull();
      expect(entry!.title).toBe("PostgreSQL 16 — Primary Database");
      expect(entry!.what).toBe("primary database is PostgreSQL 16");
      expect(entry!.why).toBe(
        "ACID guarantees, mature ecosystem, pgvector for embeddings",
      );
      expect(entry!.rejected).toBe(
        "MongoDB (no multi-doc txns), DynamoDB (no txns)",
      );
      expect(entry!.constraint).toBe(
        "financial domain requires strict consistency",
      );
      expect(entry!.tags).toEqual(["database", "architecture", "postgresql"]);
      expect(entry!.importance).toBe(0.85);
      expect(entry!.created).toBe("2026-02-16");
      expect(entry!.layer).toBe(MemoryLayer.Facts);
      expect(entry!.scope).toBe("project");
    });

    it("should parse entry with colons in timestamp values", () => {
      const mml = `### Test Entry
- **what**: test content
- **tags**: test
- **importance**: 0.5
- **created**: 2026-02-16T10:30:00Z`;

      const entry = parseEntry(mml, ...baseParams);

      expect(entry).not.toBeNull();
      expect(entry!.created).toBe("2026-02-16T10:30:00Z");
    });

    it("should parse entry with commas in why field", () => {
      const mml = `### Test Entry
- **what**: test content
- **why**: reason one, reason two, reason three
- **tags**: test
- **importance**: 0.5
- **created**: 2026-02-16`;

      const entry = parseEntry(mml, ...baseParams);

      expect(entry).not.toBeNull();
      expect(entry!.why).toBe("reason one, reason two, reason three");
    });

    it("should reject entry with missing required keys (no what)", () => {
      const mml = `### Test Entry
- **tags**: test
- **importance**: 0.5
- **created**: 2026-02-16`;

      const entry = parseEntry(mml, ...baseParams);
      expect(entry).toBeNull();
    });

    it("should reject entry with missing required keys (no tags)", () => {
      const mml = `### Test Entry
- **what**: test content
- **importance**: 0.5
- **created**: 2026-02-16`;

      const entry = parseEntry(mml, ...baseParams);
      expect(entry).toBeNull();
    });

    it("should reject entry with missing required keys (no importance)", () => {
      const mml = `### Test Entry
- **what**: test content
- **tags**: test
- **created**: 2026-02-16`;

      const entry = parseEntry(mml, ...baseParams);
      expect(entry).toBeNull();
    });

    it("should reject entry with missing required keys (no created)", () => {
      const mml = `### Test Entry
- **what**: test content
- **tags**: test
- **importance**: 0.5`;

      const entry = parseEntry(mml, ...baseParams);
      expect(entry).toBeNull();
    });

    it("should parse entry with optional episode fields", () => {
      const mml = `### Race condition in payment processing
- **what**: race condition in OrderService.ProcessPayment()
- **symptom**: duplicate charges
- **root-cause**: unsynchronized database access
- **fix**: added IsolationLevel.Serializable
- **file**: OrderService.cs
- **tags**: bug, race-condition, payment
- **importance**: 0.75
- **created**: 2026-02-17`;

      const entry = parseEntry(
        mml,
        MemoryLayer.Episodes,
        "project",
        "/test/episodes/2026-02.md",
      );

      expect(entry).not.toBeNull();
      expect(entry!.symptom).toBe("duplicate charges");
      expect(entry!["root-cause"]).toBe("unsynchronized database access");
      expect(entry!.fix).toBe("added IsolationLevel.Serializable");
      expect(entry!.file).toBe("OrderService.cs");
    });

    it("should parse entry with procedure fields", () => {
      const mml = `### API endpoint structure
- **what**: structure for API endpoints
- **do**: validate with FluentValidation, return ProblemDetails on 4xx
- **dont**: expose internal errors, use DataAnnotations
- **tags**: api, validation, pattern
- **importance**: 0.6
- **created**: 2026-02-16`;

      const entry = parseEntry(
        mml,
        MemoryLayer.Procedures,
        "project",
        "/test/procedures/api.md",
      );

      expect(entry).not.toBeNull();
      expect(entry!.do).toBe(
        "validate with FluentValidation, return ProblemDetails on 4xx",
      );
      expect(entry!.dont).toBe("expose internal errors, use DataAnnotations");
    });

    it("should generate consistent entry IDs from heading and created date", () => {
      const mml = `### Test Entry
- **what**: test content
- **tags**: test
- **importance**: 0.5
- **created**: 2026-02-16`;

      const entry1 = parseEntry(mml, ...baseParams);
      const entry2 = parseEntry(mml, ...baseParams);

      expect(entry1!.id).toBe(entry2!.id);
      expect(entry1!.id).toMatch(/^e_\d+_[a-f0-9]{4}$/);
    });
  });

  describe("parseEntries", () => {
    it("should parse multi-entry file correctly", () => {
      const fileContent = `# Database Architecture

### PostgreSQL 16 — Primary Database
- **what**: primary database is PostgreSQL 16
- **why**: ACID guarantees
- **tags**: database, postgresql
- **importance**: 0.85
- **created**: 2026-02-16

### Redis — Cache Layer
- **what**: using Redis for caching
- **why**: fast in-memory access
- **tags**: cache, redis
- **importance**: 0.70
- **created**: 2026-02-17`;

      const entries = parseEntries(
        fileContent,
        MemoryLayer.Facts,
        "project",
        "/test/facts/db.md",
      );

      expect(entries).toHaveLength(2);
      expect(entries[0].title).toBe("PostgreSQL 16 — Primary Database");
      expect(entries[1].title).toBe("Redis — Cache Layer");
    });

    it("should ignore H1/H2 category headers", () => {
      const fileContent = `# Main Category

## Subcategory

### Actual Entry
- **what**: test content
- **tags**: test
- **importance**: 0.5
- **created**: 2026-02-16`;

      const entries = parseEntries(
        fileContent,
        MemoryLayer.Facts,
        "project",
        "/test/facts/test.md",
      );

      expect(entries).toHaveLength(1);
      expect(entries[0].title).toBe("Actual Entry");
    });

    it("should handle entries with no bleeding between them", () => {
      const fileContent = `### Entry One
- **what**: first entry
- **why**: reason one
- **tags**: test1
- **importance**: 0.5
- **created**: 2026-02-16

### Entry Two
- **what**: second entry
- **why**: reason two
- **tags**: test2
- **importance**: 0.6
- **created**: 2026-02-17`;

      const entries = parseEntries(
        fileContent,
        MemoryLayer.Facts,
        "project",
        "/test/facts/test.md",
      );

      expect(entries).toHaveLength(2);
      expect(entries[0].what).toBe("first entry");
      expect(entries[0].why).toBe("reason one");
      expect(entries[1].what).toBe("second entry");
      expect(entries[1].why).toBe("reason two");
    });

    it("should skip entries with missing required fields", () => {
      const fileContent = `### Valid Entry
- **what**: valid content
- **tags**: test
- **importance**: 0.5
- **created**: 2026-02-16

### Invalid Entry
- **what**: missing tags
- **importance**: 0.5
- **created**: 2026-02-16

### Another Valid Entry
- **what**: also valid
- **tags**: test
- **importance**: 0.6
- **created**: 2026-02-17`;

      const entries = parseEntries(
        fileContent,
        MemoryLayer.Facts,
        "project",
        "/test/facts/test.md",
      );

      expect(entries).toHaveLength(2);
      expect(entries[0].title).toBe("Valid Entry");
      expect(entries[1].title).toBe("Another Valid Entry");
    });
  });

  describe("serializeEntry", () => {
    it("should serialize entry to MML format", () => {
      const entry: MemoryEntry = {
        id: "e_1234567890_abcd",
        title: "PostgreSQL 16 — Primary Database",
        fields: {
          what: "primary database is PostgreSQL 16",
          why: "ACID guarantees, mature ecosystem",
          rejected: "MongoDB, DynamoDB",
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
      };

      const serialized = serializeEntry(entry);

      expect(serialized).toContain("### PostgreSQL 16 — Primary Database");
      expect(serialized).toContain(
        "- **what**: primary database is PostgreSQL 16",
      );
      expect(serialized).toContain(
        "- **why**: ACID guarantees, mature ecosystem",
      );
      expect(serialized).toContain("- **rejected**: MongoDB, DynamoDB");
      expect(serialized).toContain(
        "- **tags**: database, architecture, postgresql",
      );
      expect(serialized).toContain("- **importance**: 0.85");
      expect(serialized).toContain("- **created**: 2026-02-16");
    });

    it("should order fields with what first", () => {
      const entry: MemoryEntry = {
        id: "e_1234567890_abcd",
        title: "Test",
        fields: {
          created: "2026-02-16",
          importance: "0.5",
          tags: "test",
          what: "test content",
        },
        what: "test content",
        tags: ["test"],
        importance: 0.5,
        created: "2026-02-16",
        layer: MemoryLayer.Facts,
        scope: "project",
        filePath: "/test/facts/test.md",
      };

      const serialized = serializeEntry(entry);
      const lines = serialized.split("\n");

      expect(lines[1]).toBe("- **what**: test content");
    });
  });

  describe("serializeEntries", () => {
    it("should serialize multiple entries with blank lines between", () => {
      const entries: MemoryEntry[] = [
        {
          id: "e_1234567890_abcd",
          title: "Entry One",
          fields: {
            what: "first entry",
            tags: "test",
            importance: "0.5",
            created: "2026-02-16",
          },
          what: "first entry",
          tags: ["test"],
          importance: 0.5,
          created: "2026-02-16",
          layer: MemoryLayer.Facts,
          scope: "project",
          filePath: "/test/facts/test.md",
        },
        {
          id: "e_1234567891_efgh",
          title: "Entry Two",
          fields: {
            what: "second entry",
            tags: "test",
            importance: "0.6",
            created: "2026-02-17",
          },
          what: "second entry",
          tags: ["test"],
          importance: 0.6,
          created: "2026-02-17",
          layer: MemoryLayer.Facts,
          scope: "project",
          filePath: "/test/facts/test.md",
        },
      ];

      const serialized = serializeEntries(entries);

      expect(serialized).toContain("### Entry One");
      expect(serialized).toContain("### Entry Two");
      expect(serialized).toMatch(/### Entry One[\s\S]*?\n\n### Entry Two/);
    });

    it("should include header if provided", () => {
      const entries: MemoryEntry[] = [
        {
          id: "e_1234567890_abcd",
          title: "Test Entry",
          fields: {
            what: "test",
            tags: "test",
            importance: "0.5",
            created: "2026-02-16",
          },
          what: "test",
          tags: ["test"],
          importance: 0.5,
          created: "2026-02-16",
          layer: MemoryLayer.Facts,
          scope: "project",
          filePath: "/test/facts/test.md",
        },
      ];

      const serialized = serializeEntries(entries, "# Database Facts");

      expect(serialized).toMatch(/^# Database Facts\n\n### Test Entry/);
    });
  });

  describe("extractHeader", () => {
    it("should extract header before first entry", () => {
      const fileContent = `# Database Architecture

This file contains database decisions.

### PostgreSQL 16 — Primary Database
- **what**: primary database is PostgreSQL 16`;

      const header = extractHeader(fileContent);

      expect(header).toContain("# Database Architecture");
      expect(header).toContain("This file contains database decisions.");
      expect(header).not.toContain("### PostgreSQL");
    });

    it("should handle file with no header", () => {
      const fileContent = `### First Entry
- **what**: test content`;

      const header = extractHeader(fileContent);

      expect(header).toBe("");
    });
  });

  describe("Round-trip: parse → serialize → parse", () => {
    it("should produce identical output after round-trip", () => {
      const originalMML = `### PostgreSQL 16 — Primary Database
- **what**: primary database is PostgreSQL 16
- **why**: ACID guarantees, mature ecosystem
- **rejected**: MongoDB, DynamoDB
- **tags**: database, architecture, postgresql
- **importance**: 0.85
- **created**: 2026-02-16`;

      const entry1 = parseEntry(
        originalMML,
        MemoryLayer.Facts,
        "project",
        "/test/facts/db.md",
      );
      expect(entry1).not.toBeNull();

      const serialized = serializeEntry(entry1!);

      const entry2 = parseEntry(
        serialized,
        MemoryLayer.Facts,
        "project",
        "/test/facts/db.md",
      );
      expect(entry2).not.toBeNull();

      expect(entry2!.title).toBe(entry1!.title);
      expect(entry2!.what).toBe(entry1!.what);
      expect(entry2!.why).toBe(entry1!.why);
      expect(entry2!.rejected).toBe(entry1!.rejected);
      expect(entry2!.tags).toEqual(entry1!.tags);
      expect(entry2!.importance).toBe(entry1!.importance);
      expect(entry2!.created).toBe(entry1!.created);
    });
  });

  describe("generateEntryId", () => {
    it("should generate consistent IDs for same heading and date", () => {
      const id1 = generateEntryId("Test Entry", "2026-02-16");
      const id2 = generateEntryId("Test Entry", "2026-02-16");

      expect(id1).toBe(id2);
    });

    it("should generate different IDs for different headings", () => {
      const id1 = generateEntryId("Entry One", "2026-02-16");
      const id2 = generateEntryId("Entry Two", "2026-02-16");

      expect(id1).not.toBe(id2);
    });

    it("should match expected format", () => {
      const id = generateEntryId("Test Entry", "2026-02-16");

      expect(id).toMatch(/^e_\d+_[a-f0-9]{4}$/);
    });
  });
});
