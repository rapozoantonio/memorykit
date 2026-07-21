# Claude Code Integration Plan — memorykit-mcp-server

Fact-checked against live Claude Code docs, July 2026.

---

## Two Goals That Must Both Be True

**1. Fewer tokens.** Claude should spend 200 tokens recalling a decision, not 2,000 re-discovering it.

**2. Retrieval must be right.** The token savings only materialise if the recalled memories are actually relevant. A bad retrieval that pulls the wrong 5 files saves tokens and ships bugs — the cost doesn't disappear, it shifts from inference spend to debugging time you don't measure. Cheap + wrong is worse than expensive + right.

Every change in this plan is only worth making if it satisfies both goals simultaneously.

---

## The Core Problem with Current Approach

Every memory MCP server today — mem0, basic-memory, and current memorykit — puts instructions in CLAUDE.md and trusts Claude to comply. "Check memory before tasks." Claude reads it, sometimes follows it, often doesn't, and always loses it after `/compact`.

This plan moves the critical path from advisory to deterministic. Not by adding more instructions, but by wiring into Claude Code's framework layer so the right things happen automatically.

---

## What `memorykit init` Must Generate

### 1. `.mcp.json` — add `alwaysLoad: true`

**Current:** `.mcp.json` is generated but missing `alwaysLoad`.

```json
{
  "mcpServers": {
    "memorykit": {
      "command": "memorykit",
      "alwaysLoad": true,
      "env": {
        "MEMORYKIT_PROJECT": "${workspaceFolder}"
      }
    }
  }
}
```

Claude Code's tool search can defer MCP tool schemas to save context space. If memorykit tools are deferred and Claude hasn't "discovered" them yet, it cannot call `retrieve_context` when it most needs to — at the start of a task. `alwaysLoad: true` keeps the tools visible from session open. No discovery delay, no silent failures.

**Why this serves both goals:** Zero token cost (schemas are prompt-cached). Tools available immediately means Claude can make precise calls the moment a task starts, not after a discovery round-trip.

---

### 2. `.claude/settings.local.json` — SessionStart hook

```json
{
  "hooks": {
    "SessionStart": [
      {
        "matcher": ".*",
        "hooks": [
          {
            "type": "mcp_tool",
            "server": "memorykit",
            "tool": "retrieve_context",
            "input": {
              "query": "what was I working on and what decisions were made",
              "scope": "all"
            }
          }
        ]
      }
    ]
  }
}
```

This fires before Claude reads the first message of a session. The result is injected into context automatically by the framework — no Claude decision required.

**Why `.claude/settings.local.json`:** Git-ignored by default. Personal, doesn't pollute the team repo. Covers all sessions and worktrees for this project from one file.

**The query matters.** `"what was I working on and what decisions were made"` classifies as `Continuation` in the Prefrontal Controller — ~200 token budget, hits `working/session.md` and recent `facts`. Cheap and scoped. It is not a broad dump of everything stored.

**Precision note:** This session-start retrieval is intentionally narrow — it loads continuity context, not deep recall. The assumption is that Claude will make more specific `retrieve_context` calls as it understands the actual task. The session start call is the warm-up, not the full load.

---

### 3. `.claude/skills/recall/SKILL.md`

```markdown
---
name: recall
description: Retrieve relevant memories from memorykit for the current task or topic. Use when starting a task, switching context, or when the user asks about past decisions, patterns, or bugs.
allowed-tools: mcp__memorykit__retrieve_context
---

Call retrieve_context with the specific task or topic as the query — be precise.
A narrow query ("auth token refresh bug") returns better results than a broad one ("authentication").
If an argument is provided (e.g. /recall auth module), use it directly as the query.
Otherwise, derive the query from what the user is currently trying to do.
Return the results without reformatting.
```

**Why precision in the skill instruction matters:** This is where goal #2 lives. The skill tells Claude to form a narrow, specific query. A `/recall` that fires with "authentication" as the query may pull 5 loosely related files. The same call with "auth token refresh bug 2026-07" pulls exactly the right episode. The instruction inside the skill shapes the query quality, which determines retrieval quality.

`allowed-tools` pre-approves `retrieve_context` for this skill's turn — no permission prompt, no friction.

---

### 4. `.claude/skills/save/SKILL.md`

```markdown
---
name: save
description: Save an important discovery, decision, or pattern to memorykit memory. Use after fixing a bug, making an architecture decision, establishing a coding convention, or completing significant work.
allowed-tools: mcp__memorykit__store_memory
---

Call store_memory with precise, self-contained content — write it as if explaining to a future
developer who has no session context. Include the WHY, not just the WHAT.

Choose the correct layer:
- facts: architecture decisions, technology choices, permanent constraints
- episodes: bugs fixed, failed approaches, incidents, root causes
- procedures: coding patterns, conventions, repeatable workflows
- working: active task context (auto-expires after 7 days)

If this discovery took real investigation (multiple tool calls, reading several files),
estimate tokens_consumed and tool_calls and pass them in acquisition_context.
This enables exact ROI measurement on retrieval.
```

**Why this serves goal #2:** Storage quality determines retrieval quality. A memory stored as "fixed the bug" retrieves poorly. A memory stored as "auth token refresh fails when user has no refresh_token field — check user.refresh_token before calling refreshSession()" retrieves precisely and saves the right tokens. The skill instruction shapes what gets written, which determines what can be found later.

---

### 5. `.claude/rules/memory.md`

Path-scoped rules load on-demand when Claude accesses files matching their patterns. Zero startup cost — they only consume tokens when relevant.

```markdown
---
paths:
  - "src/**/*"
  - "*.ts"
  - "*.py"
  - "*.go"
  - "*.rs"
---

Before modifying this file, check if retrieve_context has been called for the specific
component or module being changed. If not, call it with the filename or module name as
the query — not a generic query. Specific queries return better results.

After discovering a bug root cause or an important constraint in this code, call store_memory.
```

**Why path-scoped over startup-loaded:** The rule fires with natural context — Claude is already looking at `src/auth/token.ts`, so the query "auth token" is obvious. This is better than a generic session-start nudge because the file context provides the retrieval signal automatically. It also doesn't consume tokens unless Claude is actually touching source files.

---

## 6. MCP Server: `instructions` in InitializeResult

**Fix in `src/server.ts`:**

```typescript
const server = new Server(
  { name: "memorykit", version: _pkg.version },
  {
    capabilities: { tools: {} },
    instructions:
      "Call retrieve_context before starting any task. Use a specific, narrow query — " +
      "the name of the module, bug, or decision — not a generic topic. Specific queries " +
      "return fewer, more relevant results and cost fewer tokens than broad ones. " +
      "Call store_memory after discovering architecture decisions (facts), bugs and root " +
      "causes (episodes), or conventions (procedures). Include the WHY in stored content. " +
      "Call initialize_memory once per project if other tools return initialization errors.",
  }
);
```

This is injected into Claude at MCP handshake time — before any CLAUDE.md loads, before any user message. It's the fallback for users who never ran `memorykit init` (Claude Desktop, raw installs). Claude Code truncates at 2KB; keep this under 800 chars.

**The instruction emphasises query specificity** — same principle as the skills. The server tells Claude from the first moment that narrow queries are better than broad ones.

---

## Why PreCompact Hook Was Removed

An earlier version of this plan included a `PreCompact` hook that would save session state before `/compact` runs.

This was wrong for two reasons:

**1. It fights the mechanism.** `/compact` fires because the context window is full. Adding tool calls to a full context makes the problem worse, not better.

**2. It's the wrong layer.** If important discoveries weren't stored when they happened — when Claude found the bug, made the decision, spotted the pattern — a last-second hook can't reconstruct them reliably. It would just write a generic placeholder that adds noise to working memory.

The correct answer to "don't lose context on compact" is: store things continuously at discovery time, which is what the `/save` skill and server instructions drive. If those work, compact is just compact — a clean slate that doesn't hurt because the important stuff is already persisted.

---

## Summary: What Changes and Why

| Change | Token goal | Quality goal |
|--------|-----------|--------------|
| `alwaysLoad: true` in `.mcp.json` | No discovery latency | Tools available for precise calls immediately |
| SessionStart hook | ~200 tokens buys continuity context | Narrow `Continuation` query, not a broad dump |
| `/recall` skill with precision instruction | Claude calls it when relevant | Instructs Claude to form narrow queries |
| `/save` skill with quality instruction | Stores only what matters | Instructs Claude to write self-contained, why-focused content |
| Path-scoped rule | Only loads when touching source | File context provides natural query signal |
| Server `instructions` | Guides Claude from first moment | Emphasises specific queries over generic ones |

---

## What Developers Do After `memorykit init`

1. Restart Claude Code.
2. Nothing else.

The hook fires on session open. The tools are always visible. The skills are available for manual use and auto-trigger. The server explains itself to Claude from the handshake.

No reading docs. No manually editing JSON. No reminding Claude to check memory.
