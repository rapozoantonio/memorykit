/**
 * Tests for Prose-to-MML Normalization Pipeline (M2)
 */

import { describe, it, expect } from "vitest";
import { normalizeToMML } from "../memory/normalizer.js";
import { MemoryLayer } from "../types/memory.js";

describe("Prose-to-MML Normalization (M2)", () => {
  describe("Decision pattern normalization", () => {
    it("should normalize decision prose to MML with what/why/rejected", () => {
      const prose =
        "We decided to use PostgreSQL 16 because of ACID guarantees and pgvector support. We rejected MongoDB because it lacks multi-document transactions.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.85, [
        "database",
        "architecture",
      ]);

      expect(result.title).toContain("PostgreSQL");
      expect(result.fields.what).toBeTruthy();
      expect(result.fields.what.toLowerCase()).toContain("postgresql");
      expect(result.fields.why).toBeTruthy();
      expect(result.fields.why.toLowerCase()).toContain("acid");
      expect(result.fields.rejected).toBeTruthy();
      expect(result.fields.rejected.toLowerCase()).toContain("mongodb");
    });

    it("should extract decision entity and role for heading", () => {
      const prose = "We chose Redis as the caching layer because of its speed.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.7, ["cache"]);

      expect(result.title).toMatch(/Redis/i);
      expect(result.title.toLowerCase()).toContain("cach");
    });

    it("should handle decision with constraints", () => {
      const prose =
        "Going with Stripe for payments. Must comply with PCI DSS requirements. Rejected PayPal due to webhook reliability issues.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.8, ["payment"]);

      expect(result.fields.what.toLowerCase()).toContain("stripe");
      expect(result.fields.constraint).toBeTruthy();
      expect(result.fields.constraint.toLowerCase()).toContain("pci");
      expect(result.fields.rejected.toLowerCase()).toContain("paypal");
    });
  });

  describe("Problem pattern normalization", () => {
    it("should normalize bug report to MML with symptom and fix", () => {
      const prose =
        "Found a race condition in OrderService.ProcessPayment that causes duplicate charges. Fixed by adding IsolationLevel.Serializable to the transaction.";

      const result = normalizeToMML(prose, MemoryLayer.Episodes, 0.75, [
        "bug",
        "payment",
      ]);

      expect(result.title.toLowerCase()).toContain("race condition");
      expect(result.title).toContain("OrderService");
      expect(result.fields.what.toLowerCase()).toContain("race condition");
      expect(result.fields.symptom).toBeTruthy();
      expect(result.fields.symptom.toLowerCase()).toContain("duplicate");
      expect(result.fields.fix).toBeTruthy();
      expect(result.fields.fix.toLowerCase()).toContain("serializable");
      expect(result.fields.file).toBe("OrderService.cs");
    });

    it("should extract root cause when present", () => {
      const prose =
        "Bug in AuthMiddleware causing 401 errors. Root cause was expired JWT validation cache. Workaround is to restart the service.";

      const result = normalizeToMML(prose, MemoryLayer.Episodes, 0.7, [
        "bug",
        "auth",
      ]);

      expect(result.fields["root-cause"]).toBeTruthy();
      expect(result.fields["root-cause"].toLowerCase()).toContain("jwt");
      expect(result.fields.workaround).toBeTruthy();
      expect(result.fields.workaround.toLowerCase()).toContain("restart");
    });

    it("should generate heading with location for problems", () => {
      const prose =
        "Error in UserRepository.GetByEmail method throws NullReferenceException.";

      const result = normalizeToMML(prose, MemoryLayer.Episodes, 0.6, ["bug"]);

      expect(result.title).toContain("UserRepository");
    });
  });

  describe("Rule pattern normalization", () => {
    it("should normalize rule prose to MML with do/dont", () => {
      const prose =
        "Always validate API input with FluentValidation. Never use DataAnnotations for complex validation rules.";

      const result = normalizeToMML(prose, MemoryLayer.Procedures, 0.6, [
        "api",
        "validation",
      ]);

      expect(result.fields.what.toLowerCase()).toContain("validate");
      expect(result.fields.do).toBeTruthy();
      expect(result.fields.do.toLowerCase()).toContain("fluentvalidation");
      expect(result.fields.dont).toBeTruthy();
      expect(result.fields.dont.toLowerCase()).toContain("dataannotations");
    });

    it("should handle do-only rules", () => {
      const prose =
        "Always use async/await for database calls. Must include cancellation tokens.";

      const result = normalizeToMML(prose, MemoryLayer.Procedures, 0.5, [
        "coding",
      ]);

      expect(result.fields.do).toBeTruthy();
      expect(result.fields.do.toLowerCase()).toContain("async");
    });

    it("should extract format information", () => {
      const prose =
        "API responses must follow the ProblemDetails format from RFC 7807.";

      const result = normalizeToMML(prose, MemoryLayer.Procedures, 0.6, [
        "api",
      ]);

      expect(result.fields.format).toBeTruthy();
      expect(result.fields.format.toLowerCase()).toContain("problemdetails");
    });
  });

  describe("Generic pattern normalization", () => {
    it("should normalize generic prose with what field", () => {
      const prose = "The team discussed deployment options for staging.";

      const result = normalizeToMML(prose, MemoryLayer.Working, 0.3, [
        "deployment",
      ]);

      expect(result.fields.what).toBe(prose);
      expect(result.title).toContain("deployment");
    });

    it("should handle short generic content", () => {
      const prose = "Meeting at 3pm.";

      const result = normalizeToMML(prose, MemoryLayer.Working, 0.2, [
        "meeting",
      ]);

      expect(result.fields.what).toBe(prose);
      expect(result.title.length).toBeLessThanOrEqual(60);
    });
  });

  describe("Pre-structured MML passthrough", () => {
    it("should pass through valid MML unchanged", () => {
      const mml = `### PostgreSQL 16 — Database
- **what**: primary database is PostgreSQL 16
- **why**: ACID guarantees
- **tags**: database, postgresql
- **importance**: 0.85
- **created**: 2026-02-16`;

      const result = normalizeToMML(mml, MemoryLayer.Facts, 0.85, ["database"]);

      expect(result.title).toBe("PostgreSQL 16 — Database");
      expect(result.fields.what).toBe("primary database is PostgreSQL 16");
      expect(result.fields.why).toBe("ACID guarantees");
    });

    it("should validate and fill missing required fields in MML", () => {
      const incompleteMML = `### Test Entry
- **what**: test content
- **custom**: custom value`;

      const result = normalizeToMML(incompleteMML, MemoryLayer.Facts, 0.5, [
        "test",
      ]);

      expect(result.fields.what).toBe("test content");
      expect(result.fields.tags).toBeTruthy();
      expect(result.fields.importance).toBeTruthy();
      expect(result.fields.created).toBeTruthy();
    });
  });

  describe("Token efficiency", () => {
    it("should produce MML shorter than prose input", () => {
      const prose =
        "After discussing with the team, we have decided to go with TypeScript instead of JavaScript for this project. The main reason is type safety, which will help us catch bugs earlier in development. We also considered Flow but rejected it because of the smaller ecosystem and lack of tooling support compared to TypeScript.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.7, [
        "typescript",
        "decision",
      ]);

      // Count approximate tokens (rough estimate: 4 chars = 1 token)
      const proseTokens = prose.length / 4;
      const mmlContent = Object.values(result.fields).join(" ");
      const mmlTokens = mmlContent.length / 4;

      expect(mmlTokens).toBeLessThan(proseTokens * 0.7); // Should use ≤70% tokens
    });

    it("should avoid filler words in extracted fields", () => {
      const prose =
        "So basically, we kind of decided to use Redis I think because it's really fast and stuff.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.6, ["cache"]);

      // Extracted fields should be cleaner than original prose
      expect(result.fields.what.toLowerCase()).toContain("redis");
      expect(result.fields.what.toLowerCase()).not.toContain("basically");
      expect(result.fields.what.toLowerCase()).not.toContain("kind of");
    });
  });

  describe("Required fields", () => {
    it("should always include required fields: what, tags, importance, created", () => {
      const prose = "Using Redis for caching.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.6, ["cache"]);

      expect(result.fields.what).toBeTruthy();
      expect(result.fields.tags).toBeTruthy();
      expect(result.fields.importance).toBeTruthy();
      expect(result.fields.created).toBeTruthy();
    });

    it("should format importance as 2 decimal places", () => {
      const prose = "Test content.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.12345, [
        "test",
      ]);

      expect(result.fields.importance).toBe("0.12");
    });

    it("should format created as YYYY-MM-DD", () => {
      const prose = "Test content.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.5, ["test"]);

      expect(result.fields.created).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    });
  });

  describe("Heading generation", () => {
    it("should generate headings under 60 characters", () => {
      const longProse =
        "This is a very long piece of content that describes in great detail the architectural decision we made regarding the implementation of a new caching strategy using Redis with cluster mode and automatic failover capabilities.";

      const result = normalizeToMML(longProse, MemoryLayer.Facts, 0.7, [
        "architecture",
      ]);

      expect(result.title.length).toBeLessThanOrEqual(60);
    });

    it("should break on word boundaries when truncating", () => {
      const prose =
        "Implementation of sophisticated authentication mechanism with JWT tokens";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.7, ["auth"]);

      // Should not end mid-word
      expect(result.title).not.toMatch(/\w\.\.\.$/);
    });

    it("should capitalize headings", () => {
      const prose = "using redis for caching";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.6, ["cache"]);

      expect(result.title[0]).toMatch(/[A-Z]/);
    });
  });

  describe("Layer-specific field extraction", () => {
    it("should extract decision fields for facts layer", () => {
      const prose =
        "Choosing Next.js over Create React App. Rejected CRA due to lack of SSR.";

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.8, [
        "frontend",
      ]);

      expect(result.fields.rejected).toBeTruthy();
    });

    it("should extract episode fields for episodes layer", () => {
      const prose =
        "Fixed memory leak in WebSocket connection handler. Symptom was increasing RAM usage over time.";

      const result = normalizeToMML(prose, MemoryLayer.Episodes, 0.7, ["bug"]);

      expect(result.fields.symptom).toBeTruthy();
      expect(result.fields.fix).toBeTruthy();
    });

    it("should extract procedure fields for procedures layer", () => {
      const prose =
        "When deploying to production, always run database migrations first. Never deploy without testing in staging.";

      const result = normalizeToMML(prose, MemoryLayer.Procedures, 0.6, [
        "deployment",
      ]);

      expect(result.fields.do).toBeTruthy();
      expect(result.fields.dont).toBeTruthy();
    });
  });

  describe("Edge cases", () => {
    it("should handle empty content", () => {
      const result = normalizeToMML("", MemoryLayer.Working, 0.5, ["test"]);

      expect(result.fields.what).toBeTruthy();
      expect(result.title).toBeTruthy();
    });

    it("should handle content with no pattern match", () => {
      const prose = "Lorem ipsum dolor sit amet.";

      const result = normalizeToMML(prose, MemoryLayer.Working, 0.3, ["test"]);

      expect(result.fields.what).toBe(prose);
    });

    it("should handle multi-line content", () => {
      const prose = `We decided to use PostgreSQL.
      
Main reasons:
- ACID compliance
- Mature ecosystem
- Great tooling`;

      const result = normalizeToMML(prose, MemoryLayer.Facts, 0.8, [
        "database",
      ]);

      expect(result.fields.what).toBeTruthy();
      expect(result.title).toContain("PostgreSQL");
    });
  });
});
