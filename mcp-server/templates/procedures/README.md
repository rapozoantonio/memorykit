# Project Procedures

<!--
  Patterns, conventions, rules, and standard practices for this project.
  "Always do X", "Never do Y", "When Z happens, follow these steps".

  Entries use MML (Memory Markup Language) format.
-->

---

## Example Entry

### API endpoint structure

- **what**: pattern for all API endpoints
- **do**: validate with FluentValidation, return ProblemDetails on 4xx errors, wrap in try-catch returning 500 with correlation ID
- **dont**: expose internal error messages, use DataAnnotations validation, swallow exceptions silently
- **format**: POST /api/v1/{resource}, GET /api/v1/{resource}/{id}
- **tags**: api, validation, pattern, error-handling
- **importance**: 0.70
- **created**: 2026-02-12

### Database migration workflow

- **what**: process for applying schema changes
- **do**: create migration with 'dotnet ef migrations add Name', review SQL, test locally, apply to staging, then production
- **dont**: modify existing migrations after deployment, run migrations manually in production
- **trigger**: schema change needed
- **tags**: database, migrations, ef-core, workflow
- **importance**: 0.65
- **created**: 2026-02-09

### Git commit conventions

- **what**: commit message format for this project
- **format**: type(scope): subject - body explains why, not what. Types: feat, fix, refactor, docs, test
- **do**: reference issue numbers, write imperative mood ("add feature" not "added")
- **dont**: commit WIP changes to main, combine unrelated changes
- **tags**: git, conventions, workflow
- **importance**: 0.50
- **created**: 2026-02-05

---

**MML Keys for Procedures:**

- **what** (required): What this procedure covers
- **do** (recommended): What to do
- **dont**: What to avoid
- **format**: Specific format or structure
- **trigger**: Condition that activates this procedure
- **when**: Time or situation when this applies
- **tags** (required): Comma-separated keywords
- **importance** (auto): Calculated by Amygdala engine
- **created** (auto): Date stored
