/**
 * Integration tests for Prefrontal Controller Query Classification
 * M11: Test Suite 3 - Validates routing accuracy
 */

import { describe, it, expect } from "vitest";
import { classifyQuery } from "../cognitive/prefrontal.js";
import { QueryType } from "../types/cognitive.js";

describe("Prefrontal Query Classification Accuracy", () => {
  describe("Continuation queries", () => {
    it('should classify "continue" as continuation', () => {
      const result = classifyQuery("continue");
      expect(result.type).toBe(QueryType.Continuation);
    });

    it('should classify "yeah" as continuation', () => {
      const result = classifyQuery("yeah");
      expect(result.type).toBe(QueryType.Continuation);
    });

    it('should classify "ok" as continuation', () => {
      const result = classifyQuery("ok");
      expect(result.type).toBe(QueryType.Continuation);
    });

    it('should classify "sounds good" as continuation', () => {
      const result = classifyQuery("sounds good");
      expect(result.type).toBe(QueryType.Continuation);
    });
  });

  describe("Fact retrieval queries", () => {
    it('should classify "what database are we using?" as factRetrieval', () => {
      const result = classifyQuery("what database are we using?");
      expect(result.type).toBe(QueryType.FactRetrieval);
    });

    it('should classify "what\'s our caching strategy?" as factRetrieval', () => {
      const result = classifyQuery("what's our caching strategy?");
      expect(result.type).toBe(QueryType.FactRetrieval);
    });

    it('should classify "tell me about the auth system" as factRetrieval', () => {
      const result = classifyQuery("tell me about the auth system");
      expect(result.type).toBe(QueryType.FactRetrieval);
    });

    it("should handle red herring: ignore negated terms", () => {
      const result = classifyQuery(
        "don't worry about the database, what's our caching strategy?",
      );
      expect(result.type).toBe(QueryType.FactRetrieval);
      // Should focus on "caching" not "database"
    });
  });

  describe("Deep recall queries (historical)", () => {
    it('should classify "how did we fix the auth bug?" as deepRecall', () => {
      const result = classifyQuery("how did we fix the auth bug?");
      expect(result.type).toBe(QueryType.DeepRecall);
    });

    it('should classify "what went wrong with the deployment last week?" as deepRecall', () => {
      const result = classifyQuery(
        "what went wrong with the deployment last week?",
      );
      expect(result.type).toBe(QueryType.DeepRecall);
    });

    it('should classify "remind me about the payment bug" as deepRecall', () => {
      const result = classifyQuery("remind me about the payment bug");
      expect(result.type).toBe(QueryType.DeepRecall);
    });
  });

  describe("Procedural queries", () => {
    it('should classify "how should I structure API endpoints?" as procedural', () => {
      const result = classifyQuery("how should I structure API endpoints?");
      expect(result.type).toBe(QueryType.ProceduralTrigger);
    });

    it('should classify "what\'s the process for migrations?" as procedural', () => {
      const result = classifyQuery("what's the process for migrations?");
      expect(result.type).toBe(QueryType.ProceduralTrigger);
    });

    it('should classify "how do we handle errors?" as procedural', () => {
      const result = classifyQuery("how do we handle errors?");
      expect(result.type).toBe(QueryType.ProceduralTrigger);
    });

    it('should classify "what are our git conventions?" as procedural', () => {
      const result = classifyQuery("what are our git conventions?");
      expect(result.type).toBe(QueryType.ProceduralTrigger);
    });
  });

  describe("Complex multi-layer queries", () => {
    it('should classify "tell me about auth flow and deployment" as complex', () => {
      const result = classifyQuery("tell me about auth flow and deployment");
      expect(result.type).toBe(QueryType.Complex);
    });

    it('should classify "what should we do about the performance issues in auth?" as complex', () => {
      const result = classifyQuery(
        "what should we do about the performance issues in auth?",
      );
      expect(result.type).toBe(QueryType.Complex);
      // Ambiguous: could need facts (current auth), episodes (past issues), procedures (how to fix)
    });

    it('should classify "explain the database architecture and how we handle migrations" as complex', () => {
      const result = classifyQuery(
        "explain the database architecture and how we handle migrations",
      );
      expect(result.type).toBe(QueryType.Complex);
    });

    it("should classify vague multi-domain query as complex", () => {
      const result = classifyQuery(
        "what do we know about authentication and payments?",
      );
      expect(result.type).toBe(QueryType.Complex);
    });
  });

  describe("Edge cases", () => {
    it("should handle empty query", () => {
      const result = classifyQuery("");
      expect(result.type).toBe(QueryType.Continuation);
    });

    it("should handle very short query", () => {
      const result = classifyQuery("k");
      expect(result.type).toBe(QueryType.Continuation);
    });

    it("should handle query with only stop words", () => {
      const result = classifyQuery("the and or but");
      expect(result.type).toBe(QueryType.Continuation);
    });

    it("should classify specific technical query correctly", () => {
      const result = classifyQuery(
        "what's the pgvector configuration for embeddings?",
      );
      expect(result.type).toBe(QueryType.FactRetrieval);
    });

    it("should handle question about past decision process", () => {
      const result = classifyQuery(
        "why did we choose PostgreSQL over MongoDB?",
      );
      // Could be factRetrieval (decision stored) or deepRecall (past discussion)
      // Both are acceptable
      expect([QueryType.FactRetrieval, QueryType.DeepRecall]).toContain(
        result.type,
      );
    });
  });

  describe("Routing accuracy metrics", () => {
    const testCases = [
      { query: "continue", expected: QueryType.Continuation },
      {
        query: "what database are we using?",
        expected: QueryType.FactRetrieval,
      },
      { query: "how did we fix the auth bug?", expected: QueryType.DeepRecall },
      {
        query: "how should I structure API endpoints?",
        expected: QueryType.ProceduralTrigger,
      },
      {
        query: "tell me about auth flow and deployment",
        expected: QueryType.Complex,
      },
      {
        query: "don't worry about the database, what's our caching strategy?",
        expected: QueryType.FactRetrieval,
      },
      { query: "yeah", expected: QueryType.Continuation },
      {
        query: "what should we do about the performance issues in auth?",
        expected: QueryType.Complex,
      },
    ];

    it("should achieve ≥80% routing accuracy on test queries", () => {
      let correct = 0;

      for (const { query, expected } of testCases) {
        const result = classifyQuery(query);
        if (result.type === expected) {
          correct++;
        }
      }

      const accuracy = correct / testCases.length;

      // Target: 80% accuracy (6.4/8 = 80%)
      expect(accuracy).toBeGreaterThanOrEqual(0.8);
      console.log(
        `Prefrontal routing accuracy: ${(accuracy * 100).toFixed(1)}% (${correct}/${testCases.length})`,
      );
    });
  });

  describe("Confidence scoring", () => {
    it("should return high confidence for clear continuation", () => {
      const result = classifyQuery("yes");
      expect(result.confidence).toBeGreaterThan(0.8);
    });

    it("should return high confidence for explicit fact query", () => {
      const result = classifyQuery("what is our database?");
      expect(result.confidence).toBeGreaterThan(0.7);
    });

    it("should return lower confidence for ambiguous query", () => {
      const result = classifyQuery("something about the thing");
      expect(result.confidence).toBeLessThan(0.6);
    });
  });
});
