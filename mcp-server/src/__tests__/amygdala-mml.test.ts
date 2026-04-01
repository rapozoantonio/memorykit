/**
 * Tests for Amygdala MML Structure Detection (M5)
 */

import { describe, it, expect } from "vitest";
import { calculateImportance } from "../cognitive/amygdala.js";

describe("Amygdala MML Adaptation (M5)", () => {
  describe("MML Structure Detection", () => {
    it("should score MML decision with rejected field highly", () => {
      const mmlContent = `### PostgreSQL 16 — Primary Database
- **what**: primary database is PostgreSQL 16
- **why**: ACID guarantees, mature ecosystem, pgvector for embeddings
- **rejected**: MongoDB (no multi-doc txns), DynamoDB (no txns)
- **tags**: database, architecture, postgresql`;

      const score = calculateImportance(mmlContent);

      // MML structure signal 0.7, weighted 70%, combined with other signals
      expect(score).toBeGreaterThanOrEqual(0.45);
      expect(score).toBeLessThanOrEqual(0.75);
    });

    it("should score MML bug with symptom + fix appropriately", () => {
      const mmlContent = `### Race condition in payment processing
- **what**: race condition in OrderService.ProcessPayment()
- **symptom**: duplicate charges appearing in production
- **fix**: added IsolationLevel.Serializable to transaction
- **root-cause**: concurrent requests modifying same order row
- **tags**: bug, race-condition, payment`;

      const score = calculateImportance(mmlContent);

      // MML structure signal 0.6, weighted 70%, combined with other signals
      expect(score).toBeGreaterThanOrEqual(0.4);
      expect(score).toBeLessThanOrEqual(0.7);
    });

    it("should score MML procedure with do + dont", () => {
      const mmlContent = `### API endpoint structure
- **what**: pattern for all API endpoints
- **do**: validate with FluentValidation, return ProblemDetails on 4xx
- **dont**: expose internal errors, use DataAnnotations
- **tags**: api, validation, pattern`;

      const score = calculateImportance(mmlContent);

      // MML structure signal 0.5, weighted 70%, combined with other signals
      expect(score).toBeGreaterThanOrEqual(0.3);
      expect(score).toBeLessThanOrEqual(0.65);
    });

    it("should score prose decision similarly to MML decision", () => {
      const proseContent =
        "We decided to use PostgreSQL 16 as the primary database because of CRITICAL ACID guarantees, mature ecosystem, and pgvector for embeddings. We rejected MongoDB due to lack of multi-document transactions and DynamoDB due to no ACID support. This decision is important for our financial domain requirements.";

      const mmlContent = `### PostgreSQL 16 — Primary Database
- **what**: primary database is PostgreSQL 16
- **why**: ACID guarantees, mature ecosystem, pgvector for embeddings
- **rejected**: MongoDB (no multi-doc txns)
- **tags**: database`;

      const proseScore = calculateImportance(proseContent);
      const mmlScore = calculateImportance(mmlContent);

      // Both should be substantial decisions
      // Difference should not be too large (within 0.3)
      expect(Math.abs(proseScore - mmlScore)).toBeLessThanOrEqual(0.3);
    });

    it("should score low-value prose appropriately", () => {
      const lowValueContent = "let me think about this for a moment";

      const score = calculateImportance(lowValueContent);

      expect(score).toBeGreaterThanOrEqual(0.1);
      expect(score).toBeLessThanOrEqual(0.3);
    });

    it("should boost MML with CRITICAL marker", () => {
      const mmlContent = `### Security vulnerability
- **what**: CRITICAL: SQL injection in user search endpoint
- **symptom**: unescaped user input in WHERE clause
- **fix**: switched to parameterized queries
- **tags**: security, critical, sql-injection`;

      const score = calculateImportance(mmlContent);

      // MML structure 0.6 + explicit importance boost from CRITICAL
      expect(score).toBeGreaterThanOrEqual(0.4);
      expect(score).toBeLessThanOrEqual(0.7);
    });

    it("should return low score for plain prose without structure", () => {
      const plainContent = "ok sounds good thanks";

      const score = calculateImportance(plainContent);

      expect(score).toBeLessThanOrEqual(0.2);
    });

    it("should handle partial MML structure (do only)", () => {
      const partialMMLContent = `### Code style preference
- **what**: always use async/await
- **do**: use async/await for all asynchronous operations
- **tags**: code-style, async`;

      const score = calculateImportance(partialMMLContent);

      // MML structure signal 0.3 for partial procedure
      expect(score).toBeGreaterThanOrEqual(0.2);
      expect(score).toBeLessThanOrEqual(0.5);
    });

    it("should not double-count MML structure and prose patterns", () => {
      // Content with both MML structure AND prose patterns should not score higher
      // than pure MML or pure prose with same information
      const hybridContent = `### PostgreSQL 16 — Important Decision
- **what**: we decided to use PostgreSQL 16 (CRITICAL decision)
- **rejected**: MongoDB
- **tags**: database`;

      const pureContent = `### PostgreSQL 16 — Primary Database
- **what**: primary database is PostgreSQL 16
- **rejected**: MongoDB
- **tags**: database`;

      const hybridScore = calculateImportance(hybridContent);
      const pureScore = calculateImportance(pureContent);

      // Hybrid should NOT score significantly higher (within 0.15)
      expect(Math.abs(hybridScore - pureScore)).toBeLessThanOrEqual(0.15);
    });
  });

  describe("Scoring Consistency", () => {
    it("should maintain relative ordering across formats", () => {
      const trivial = "ok";
      const factual =
        "The primary database for this project is PostgreSQL 16, which provides ACID compliance and strong consistency guarantees. The system architecture uses pgvector extension for semantic embeddings, enabling vector similarity search. Authentication is implemented with Supabase JWT tokens containing tenant_id claims for multi-tenant row-level isolation across organizations. This technical stack was selected because it provides enterprise-grade reliability.";
      const decisionWithReason = `### Database Choice
- **what**: primary database is PostgreSQL 16
- **why**: ACID guarantees needed for financial domain
- **rejected**: MongoDB
- **tags**: database`;

      const trivialScore = calculateImportance(trivial);
      const factualScore = calculateImportance(factual);
      const decisionScore = calculateImportance(decisionWithReason);

      // Verify relative ordering (absolute thresholds unreliable with geometric mean)
      expect(factualScore).toBeGreaterThan(trivialScore);
      expect(decisionScore).toBeGreaterThan(factualScore);
      expect(decisionScore).toBeGreaterThan(trivialScore);
    });
  });
});
