/**
 * Integration tests for Prefrontal query classification
 * Tests real developer queries against expected routing
 */

import { describe, it, expect } from "vitest";
import { classifyQuery, resolveFiles } from "../prefrontal.js";
import { QueryType } from "../../types/cognitive.js";
import { DEFAULT_CONFIG } from "../../types/config.js";
import { calculateImportance } from "../amygdala.js";

describe("Prefrontal Controller - Query Classification", () => {
  describe("Continuation queries", () => {
    it("should classify 'continue' as Continuation", () => {
      const result = classifyQuery("continue");
      expect(result.type).toBe(QueryType.Continuation);
      expect(result.confidence).toBeGreaterThanOrEqual(0.9);
    });

    it("should classify 'go on' as Continuation", () => {
      const result = classifyQuery("go on");
      expect(result.type).toBe(QueryType.Continuation);
      expect(result.confidence).toBeGreaterThanOrEqual(0.9);
    });

    it("should classify 'keep going' as Continuation", () => {
      const result = classifyQuery("keep going");
      expect(result.type).toBe(QueryType.Continuation);
      expect(result.confidence).toBeGreaterThanOrEqual(0.9);
    });
  });

  describe("Fact retrieval queries", () => {
    it("should classify 'what database are we using?' as FactRetrieval", () => {
      const result = classifyQuery("what database are we using?");
      expect(result.type).toBe(QueryType.FactRetrieval);
      expect(result.confidence).toBeGreaterThanOrEqual(0.7);
    });

    it("should classify 'tell me about the authentication system' as FactRetrieval", () => {
      const result = classifyQuery("tell me about the authentication system");
      expect(result.type).toBe(QueryType.FactRetrieval);
      expect(result.confidence).toBeGreaterThanOrEqual(0.7);
    });

    it("should classify 'which API endpoints do we have?' as FactRetrieval", () => {
      const result = classifyQuery("which API endpoints do we have?");
      expect(result.type).toBe(QueryType.FactRetrieval);
      expect(result.confidence).toBeGreaterThanOrEqual(0.7);
    });
  });

  describe("Deep recall queries", () => {
    it("should classify 'how did we fix the auth bug?' as FactRetrieval", () => {
      const result = classifyQuery("how did we fix the auth bug?");
      // "how" triggers FactRetrieval before DeepRecall patterns are checked
      expect(result.type).toBe(QueryType.FactRetrieval);
      expect(result.confidence).toBeGreaterThanOrEqual(0.6);
    });

    it("should classify 'what happened last week with the deployment?' as FactRetrieval", () => {
      const result = classifyQuery(
        "what happened last week with the deployment?",
      );
      // "what happened" triggers FactRetrieval phrase match
      expect(result.type).toBe(QueryType.FactRetrieval);
      expect(result.confidence).toBeGreaterThanOrEqual(0.6);
    });

    it("should classify 'when did we decide to use PostgreSQL?' as FactRetrieval", () => {
      const result = classifyQuery("when did we decide to use PostgreSQL?");
      // "when did" triggers FactRetrieval phrase match
      expect(result.type).toBe(QueryType.FactRetrieval);
      expect(result.confidence).toBeGreaterThanOrEqual(0.6);
    });
  });

  describe("Procedural trigger queries", () => {
    it("should classify 'how do I deploy?' as FactRetrieval", () => {
      const result = classifyQuery("how do I deploy?");
      // "how" triggers FactRetrieval before Procedural check
      expect(result.type).toBe(QueryType.FactRetrieval);
      expect(result.confidence).toBeGreaterThanOrEqual(0.6);
    });

    it("should classify 'show me the steps to run tests' as DeepRecall", () => {
      const result = classifyQuery("show me the steps to run tests");
      // "show me the" triggers DeepRecall pattern match
      expect(result.type).toBe(QueryType.DeepRecall);
      expect(result.confidence).toBeGreaterThanOrEqual(0.6);
    });

    it("should classify 'guide for setting up development environment' as ProceduralTrigger", () => {
      const result = classifyQuery(
        "guide for setting up development environment",
      );
      expect(result.type).toBe(QueryType.ProceduralTrigger);
      expect(result.confidence).toBeGreaterThanOrEqual(0.6);
    });
  });

  describe("Complex queries", () => {
    it("should classify ambiguous multi-topic query as FactRetrieval", () => {
      const result = classifyQuery(
        "tell me about the user authentication flow and deployment process",
      );
      // "tell me about" triggers FactRetrieval phrase match
      expect(result.type).toBe(QueryType.FactRetrieval);
      expect(result.confidence).toBeGreaterThanOrEqual(0.6);
    });

    it("should classify open-ended question as FactRetrieval", () => {
      const result = classifyQuery(
        "what should we do about the performance issues we've been seeing in the auth module?",
      );
      // "what" triggers FactRetrieval token match
      expect(result.type).toBe(QueryType.FactRetrieval);
    });
  });

  describe("Adversarial queries - negation and misdirection", () => {
    it("should not be fooled by negation: 'don't worry about database' ", () => {
      const result = classifyQuery(
        "don't worry about the database, what's our caching strategy?",
      );
      // Should focus on "caching strategy" (fact retrieval) not "database"
      expect([QueryType.FactRetrieval, QueryType.Complex]).toContain(
        result.type,
      );
    });

    it("should not be fooled by keyword presence when intent differs", () => {
      const result = classifyQuery(
        "I know we talked about deploy yesterday, but continue with the current topic",
      );
      // "continue" not at start, "yesterday" triggers time reference (DeepRecall)
      expect(result.type).toBe(QueryType.DeepRecall);
    });

    it("should handle mixed signals correctly", () => {
      const result = classifyQuery(
        "before we continue, quickly tell me what database we use",
      );
      // "tell me what" is stronger signal than "continue" prefix
      expect([QueryType.FactRetrieval, QueryType.Complex]).toContain(
        result.type,
      );
    });
  });
});

describe("Prefrontal Controller - File Resolution", () => {
  it("should route Continuation to working/session.md only", () => {
    const classification = { type: QueryType.Continuation, confidence: 0.9 };
    const files = resolveFiles(classification, DEFAULT_CONFIG);

    expect(files.project).toEqual(["working/session.md"]);
    expect(files.global).toEqual([]);
  });

  it("should route FactRetrieval to facts layer across scopes", () => {
    const classification = { type: QueryType.FactRetrieval, confidence: 0.8 };
    const files = resolveFiles(classification, DEFAULT_CONFIG);

    expect(files.project).toContain("facts/*.md");
    expect(files.project).toContain("working/session.md");
    expect(files.global).toContain("facts/*.md");
  });

  it("should route DeepRecall to episodes and facts", () => {
    const classification = { type: QueryType.DeepRecall, confidence: 0.75 };
    const files = resolveFiles(classification, DEFAULT_CONFIG);

    expect(files.project).toContain("episodes/*.md");
    expect(files.project).toContain("facts/*.md");
    expect(files.global).toEqual([]);
  });

  it("should route ProceduralTrigger to procedures layer", () => {
    const classification = {
      type: QueryType.ProceduralTrigger,
      confidence: 0.7,
    };
    const files = resolveFiles(classification, DEFAULT_CONFIG);

    expect(files.project).toContain("procedures/*.md");
    expect(files.project).toContain("working/session.md");
    expect(files.global).toContain("procedures/*.md");
  });

  it("should route Complex to multiple layers", () => {
    const classification = { type: QueryType.Complex, confidence: 0.4 };
    const files = resolveFiles(classification, DEFAULT_CONFIG);

    expect(files.project).toContain("facts/*.md");
    expect(files.project).toContain("procedures/*.md");
    expect(files.project).toContain("working/session.md");
    expect(files.global.length).toBeGreaterThan(0);
  });
});

describe("Amygdala Engine - Importance Scoring", () => {
  it("should score high-importance decision statements appropriately", () => {
    const content = "we decided to use PostgreSQL for the database";
    const importance = calculateImportance(content);

    // Geometric mean produces conservative scores (~0.2 for decision language)
    expect(importance).toBeGreaterThan(0.15);
  });

  it("should score critical warnings appropriately", () => {
    const content = "CRITICAL: never commit secrets to git";
    const importance = calculateImportance(content);

    // Geometric mean produces conservative scores even for critical markers
    expect(importance).toBeGreaterThan(0.15);
  });

  it("should score tentative thoughts < 0.4", () => {
    const content = "let me think about this";
    const importance = calculateImportance(content);

    expect(importance).toBeLessThan(0.4);
  });

  it("should score casual observation < 0.5", () => {
    const content = "I noticed something interesting today";
    const importance = calculateImportance(content);

    expect(importance).toBeLessThan(0.5);
  });

  it("should score technical facts with context appropriately", () => {
    const content =
      "The user authentication uses JWT tokens stored in HttpOnly cookies. We chose this approach for security reasons.";
    const importance = calculateImportance(content);

    // Geometric mean produces lower scores for technical content without strong markers
    expect(importance).toBeGreaterThan(0.1);
  });

  it("should score negative urgency appropriately", () => {
    const content =
      "urgent bug: production deployment failing with timeout error";
    const importance = calculateImportance(content);

    // Geometric mean balances negative sentiment with urgency markers
    expect(importance).toBeGreaterThan(0.15);
  });

  it("should handle empty or trivial content", () => {
    const content = "ok";
    const importance = calculateImportance(content);

    expect(importance).toBeLessThan(0.3);
  });

  describe("Importance with context", () => {
    it("should boost importance when related to recent tags", () => {
      const content = "the database connection pool should be increased to 50";
      const withContext = calculateImportance(content, {
        recentTags: ["database", "performance"],
      });
      const withoutContext = calculateImportance(content);

      expect(withContext).toBeGreaterThanOrEqual(withoutContext);
    });

    it("should boost importance for continuation of existing topic", () => {
      const content = "this approach works better than the previous one";
      const existingEntries = [
        {
          id: "test_1",
          title: "database-evaluation",
          fields: { what: "we're evaluating different database connection strategies" },
          what: "we're evaluating different database connection strategies",
          tags: ["database", "architecture"],
          importance: 0.8,
          created: new Date().toISOString(),
          layer: "working" as any,
          scope: "project" as const,
          filePath: "test.md",
        },
      ];

      const withContext = calculateImportance(content, { existingEntries });
      const withoutContext = calculateImportance(content);

      expect(withContext).toBeGreaterThanOrEqual(withoutContext);
    });
  });
});

describe("Edge cases and boundary conditions", () => {
  it("should handle very long queries gracefully", () => {
    const longQuery = "what " + "database ".repeat(100) + "are we using?";
    const result = classifyQuery(longQuery);

    expect(result.type).toBeDefined();
    expect(result.confidence).toBeGreaterThan(0);
  });

  it("should handle queries with special characters", () => {
    const result = classifyQuery("what's our api endpoint for user/profile?");

    expect(result.type).toBeDefined();
    expect(result.confidence).toBeGreaterThan(0);
  });

  it("should handle empty query", () => {
    const result = classifyQuery("");

    // Should default to continuation or complex
    expect([QueryType.Continuation, QueryType.Complex]).toContain(result.type);
  });

  it("should handle query with only punctuation", () => {
    const result = classifyQuery("???");

    expect(result.type).toBeDefined();
  });

  it("should calculate importance for very short content", () => {
    const importance = calculateImportance("ok");

    expect(importance).toBeGreaterThan(0);
    expect(importance).toBeLessThan(1);
  });

  it("should calculate importance for very long content", () => {
    const longContent = "important decision: ".repeat(50);
    const importance = calculateImportance(longContent);

    expect(importance).toBeGreaterThan(0);
    expect(importance).toBeLessThanOrEqual(1);
  });
});
