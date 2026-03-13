# Project Facts

<!--
  Stable knowledge about this project: technology decisions, architecture choices, constraints.
  Facts are durable and rarely change. If a fact becomes outdated, update or delete it.

  Entries use MML (Memory Markup Language) format.
-->

---

## Example Entry

### PostgreSQL 16 — Primary Database

- **what**: primary database is PostgreSQL 16
- **why**: ACID guarantees, mature ecosystem, pgvector for future embedding support
- **rejected**: MongoDB (no multi-doc transactions), DynamoDB (limited transaction support)
- **constraint**: financial domain requires strict consistency
- **tags**: database, architecture, postgresql
- **importance**: 0.85
- **created**: 2026-02-16

### Supabase JWT — Authentication

- **what**: authentication uses Supabase JWT tokens with tenant_id claim
- **why**: multi-tenant row-level security, managed service reduces maintenance
- **how**: custom middleware validates tokens in AuthMiddleware.cs
- **tags**: auth, supabase, jwt, multi-tenant
- **importance**: 0.80
- **created**: 2026-02-15

---

**MML Keys for Facts:**

- **what** (required): What this fact describes
- **why** (recommended): Rationale for decision
- **rejected**: Alternatives considered but not chosen
- **constraint**: External requirement that influenced decision
- **how**: Implementation approach
- **tags** (required): Comma-separated keywords
- **importance** (auto): Calculated by Amygdala engine
- **created** (auto): Date stored
