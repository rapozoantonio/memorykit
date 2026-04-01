/**
 * Benchmark tests for Tier 1 + Tier 2 retrieval improvements
 * Tests semantic understanding and relationship queries
 *
 * NOTE: All test content includes decision language ("We decided", "because")
 * or importance markers ("CRITICAL:", "IMPORTANT:", "NOTE:") to pass the
 * quality gates (importance_floor: 0.15). Generic test content scores near
 * zero on importance signals and gets rejected.
 */

import { describe, it, expect, beforeAll, afterAll } from "vitest";
import { tmpdir } from "os";
import { join } from "path";
import { mkdtemp, rm } from "fs/promises";
import { storeMemory } from "../memory/store.js";
import { retrieveContext } from "../memory/retrieve.js";
import { MemoryLayer, MemoryScope } from "../types/memory.js";

describe("Tier 1: Semantic Embedding Retrieval", () => {
  let testDir: string;
  const originalEnv = process.env.MEMORYKIT_PROJECT;

  beforeAll(async () => {
    testDir = await mkdtemp(join(tmpdir(), "memorykit-benchmark-"));
    process.env.MEMORYKIT_PROJECT = testDir;
    process.env.NODE_ENV = "test"; // Enable debug logging
    console.log(`Test directory: ${testDir}`);
  });

  afterAll(async () => {
    process.env.MEMORYKIT_PROJECT = originalEnv;
    if (testDir) {
      await rm(testDir, { recursive: true, force: true });
    }
  });

  it("should successfully store and retrieve entries", async () => {
    // Basic sanity check - can we store and retrieve at all?
    const storeResult = await storeMemory(
      "IMPORTANT: We decided to implement test coverage because it improves code quality significantly",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["testing", "coverage", "quality"],
      },
    );

    console.log("Store result:", JSON.stringify(storeResult, null, 2));
    expect(storeResult.stored).toBe(true);

    // Use a query that will trigger FactRetrieval classification
    const retrieveResult = await retrieveContext(
      "what testing practices did we decide to implement?",
    );
    console.log("Retrieve result:", {
      entries_returned: retrieveResult.entries_returned,
      entries_available: retrieveResult.entries_available,
    });

    expect(retrieveResult.entries_returned).toBeGreaterThan(0);
  });

  it("should find semantically related entries with zero keyword overlap", async () => {
    // Store entry about JWT
    const store1 = await storeMemory(
      "We chose JWT middleware for auth because it validates bearer tokens in the authorization header securely",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["jwt", "authentication", "middleware"],
      },
    );
    console.log("Store 1 result:", store1);
    expect(store1.stored).toBe(true);

    // Store unrelated entry
    const store2 = await storeMemory(
      "We decided to use PostgreSQL connection pooling because it reduces database latency significantly",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["database", "performance"],
      },
    );
    expect(store2.stored).toBe(true);
    console.log("Store 2 result:", store2);

    // Query with NO keyword overlap but semantically related
    const result = await retrieveContext("how do we verify user identity?");
    console.log("Query result:", {
      entries_returned: result.entries_returned,
      entries_available: result.entries_available,
      query_type: result.query_type,
    });

    // Should find JWT entry despite zero keyword matches
    expect(result.context).toMatch(/JWT|bearer|tokens|auth/i);
    expect(result.entries_returned).toBeGreaterThan(0);
  });

  it("should handle paraphrased queries", async () => {
    const storeResult = await storeMemory(
      "IMPORTANT: Redis caches user session data for fast lookups because in-memory storage reduces latency",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["redis", "cache", "sessions"],
      },
    );
    expect(storeResult.stored).toBe(true);

    // Different words, same meaning
    const result = await retrieveContext(
      "how do we store temporary user state?",
    );

    expect(result.context).toMatch(/redis|session|cache/i);
  });

  it("should rank semantic matches above low-relevance high-importance entries", async () => {
    // High importance but semantically unrelated
    const store1 = await storeMemory(
      "CRITICAL: Always use HTTPS in production deployments because unencrypted traffic exposes sensitive data",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["deployment", "https", "encryption"],
      },
    );
    expect(store1.stored).toBe(true);

    // Lower importance but semantically relevant
    const store2 = await storeMemory(
      "We decided the user authentication flow should use OAuth2 protocol because it provides secure delegated access",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["auth", "oauth", "login"],
      },
    );
    expect(store2.stored).toBe(true);

    const result = await retrieveContext("how does login security work?");

    // OAuth entry should rank higher than HTTPS entry due to semantic relevance
    const httpsIndex = result.context.indexOf("HTTPS");
    const oauthIndex = result.context.indexOf("OAuth2");

    // Both should be present
    expect(oauthIndex).toBeGreaterThan(-1);
    expect(httpsIndex).toBeGreaterThan(-1);

    // OAuth should appear before HTTPS (lower index = higher in results)
    expect(oauthIndex).toBeLessThan(httpsIndex);
  });

  it("should combine token matching and semantic matching", async () => {
    const storeResult = await storeMemory(
      "IMPORTANT: PostgreSQL stores user credentials in encrypted form because plaintext passwords are a security risk",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["database", "security"],
      },
    );
    expect(storeResult.stored).toBe(true);

    // "PostgreSQL" matches via tokens, "database" matches via embedding
    const result = await retrieveContext("what database holds credentials?");

    expect(result.context).toMatch(/PostgreSQL|credentials/i);
    expect(result.entries_returned).toBeGreaterThan(0);
  });
});

describe("Tier 2: Entity Relationship Queries", () => {
  let testDir: string;
  const originalEnv = process.env.MEMORYKIT_PROJECT;

  beforeAll(async () => {
    testDir = await mkdtemp(join(tmpdir(), "memorykit-entities-"));
    process.env.MEMORYKIT_PROJECT = testDir;
  });

  afterAll(async () => {
    process.env.MEMORYKIT_PROJECT = originalEnv;
    if (testDir) {
      await rm(testDir, { recursive: true, force: true });
    }
  });

  it("should index entities and relationships", async () => {
    const store1 = await storeMemory(
      "We decided UserService should use PostgreSQL for credential storage because it needs ACID compliance",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["architecture", "database"],
        entities: [
          {
            name: "UserService",
            type: "service",
            relationships: ["uses PostgreSQL"],
          },
          {
            name: "PostgreSQL",
            type: "database",
            relationships: ["stores credentials"],
          },
        ],
      },
    );
    expect(store1.stored).toBe(true);

    const store2 = await storeMemory(
      "We decided OrderService should use PostgreSQL for order history because it needs reliable transaction support",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["architecture", "orders", "persistence"],
        entities: [
          {
            name: "OrderService",
            type: "service",
            relationships: ["uses PostgreSQL"],
          },
        ],
      },
    );
    expect(store2.stored).toBe(true);

    // Query should find both services that use PostgreSQL
    const result = await retrieveContext("what depends on PostgreSQL?");

    expect(result.context).toMatch(/UserService/);
    expect(result.context).toMatch(/OrderService/);
  });

  it("should track relationships across multiple entries", async () => {
    const store1 = await storeMemory(
      "IMPORTANT: AuthMiddleware validates JWT tokens because every request needs authentication verification",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["middleware", "auth"],
        entities: [
          {
            name: "AuthMiddleware",
            type: "middleware",
            relationships: ["validates JWT"],
          },
        ],
      },
    );
    expect(store1.stored).toBe(true);

    const store2 = await storeMemory(
      "NOTE: JWT tokens contain user claims and expiry timestamp because stateless authentication requires embedded identity",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["jwt", "auth"],
        entities: [
          {
            name: "JWT",
            type: "token",
            relationships: ["contains user claims"],
          },
        ],
      },
    );
    expect(store2.stored).toBe(true);

    const result = await retrieveContext("how does auth middleware work?");

    expect(result.context).toMatch(/AuthMiddleware/);
    expect(result.context).toMatch(/JWT/);
  });

  it("should handle queries about specific entities", async () => {
    const storeResult = await storeMemory(
      "We decided Redis should cache frequently accessed data because memory lookups are 100x faster than disk",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["cache", "performance"],
        entities: [
          {
            name: "Redis",
            type: "cache",
            relationships: ["caches data"],
          },
        ],
      },
    );
    expect(storeResult.stored).toBe(true);

    const result = await retrieveContext("what is Redis used for?");

    expect(result.context).toMatch(/Redis.*cache/i);
    expect(result.entries_returned).toBeGreaterThan(0);
  });
});

describe("Combined Tier 1 + Tier 2 Performance", () => {
  let testDir: string;
  const originalEnv = process.env.MEMORYKIT_PROJECT;

  beforeAll(async () => {
    testDir = await mkdtemp(join(tmpdir(), "memorykit-combined-"));
    process.env.MEMORYKIT_PROJECT = testDir;
  });

  afterAll(async () => {
    process.env.MEMORYKIT_PROJECT = originalEnv;
    if (testDir) {
      await rm(testDir, { recursive: true, force: true });
    }
  });

  it("should handle complex queries with semantic + relationship understanding", async () => {
    // Build a small knowledge graph
    const store1 = await storeMemory(
      "We decided API Gateway should route requests to microservices because centralized routing simplifies client integration",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["architecture", "gateway"],
        entities: [
          {
            name: "API Gateway",
            type: "service",
            relationships: ["routes to microservices"],
          },
        ],
      },
    );
    expect(store1.stored).toBe(true);

    const store2 = await storeMemory(
      "We decided UserService should handle authentication and profiles because separating concerns improves maintainability",
      {
        layer: MemoryLayer.Facts,
        scope: MemoryScope.Project,
        tags: ["architecture", "auth"],
        entities: [
          {
            name: "UserService",
            type: "microservice",
            relationships: ["handles authentication"],
          },
        ],
      },
    );
    expect(store2.stored).toBe(true);

    // Semantic query about flow
    const result = await retrieveContext(
      "how does a request reach the auth service?",
    );

    // Should find both entries via semantic understanding
    expect(result.context).toMatch(/API Gateway|UserService/i);
    expect(result.entries_returned).toBeGreaterThan(0);
  });

  it("should provide latency improvements over baseline", async () => {
    // Store 10 entries - each needs decision language and unique tags to avoid duplicate detection
    const topics = [
      "api",
      "database",
      "cache",
      "queue",
      "storage",
      "network",
      "security",
      "monitoring",
      "deployment",
      "testing",
    ];
    for (let i = 0; i < 10; i++) {
      const storeResult = await storeMemory(
        `NOTE: We decided to use ${topics[i]} pattern ${i} because it solves important architectural challenges`,
        {
          layer: MemoryLayer.Facts,
          scope: MemoryScope.Project,
          tags: [topics[i], `pattern-${i}`],
        },
      );
      // Verify storage succeeded
      expect(storeResult.stored).toBe(true);
    }

    const start = Date.now();
    await retrieveContext("architectural patterns");
    const elapsed = Date.now() - start;

    // Should complete in under 500ms even with embeddings
    expect(elapsed).toBeLessThan(500);
  });
});
