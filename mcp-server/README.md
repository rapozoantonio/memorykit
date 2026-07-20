# MemoryKit MCP Server

**Cognitive memory for AI coding assistants** — gives Claude Desktop, Claude Code, GitHub Copilot, and Cursor persistent memory across conversations. No database, no Docker, no API keys required.

---

## The Memory That Pays for Itself

Most memory tools store everything and make you pay full re-discovery cost on every retrieval. MemoryKit doesn't:

- **Costs scale with the question, not your history** — a quick check-in costs ~200 tokens, a deep recall costs up to 1,500. Never the whole store.
- **Junk never gets stored** — low-value and duplicate entries are rejected before they're written, so you're not paying to filter noise later.
- **It shrinks itself** — stale entries auto-prune, important ones auto-promote. No manual cleanup, no slow bloat after months of use.
- **You see the receipt** — every retrieval reports real numbers: tokens spent learning a fact vs. tokens spent recalling it.

| You ask                    | Context budget |
| -------------------------- | -------------- |
| "what was I doing?"        | ~200 tokens    |
| "how do I deploy this?"    | ~300 tokens    |
| "what's our DB choice?"    | ~500 tokens    |
| "what happened last week?" | ~1,500 tokens  |

_Example: spend 1,200 tokens figuring something out once, recall it for ~70 tokens later — MemoryKit reports that 94% efficiency gain back to you, every time._

## Accurate, Not Just Fast

- **Understands meaning, not just keywords** — semantic search catches paraphrased questions a plain keyword match would miss.
- **Recent + important wins over "kinda similar"** — stale notes don't outrank what actually matters right now.
- **Catches contradictions** — conflicting info gets flagged, not silently duplicated.

---

## How It Works

MemoryKit stores memories as Markdown files on your local filesystem using a brain-inspired 4-layer architecture:

| Layer          | What it stores                             | Lifetime                              |
| -------------- | ------------------------------------------ | ------------------------------------- |
| **Working**    | Active session context, in-progress tasks  | Short-lived (decays after 7 days)     |
| **Facts**      | Architecture decisions, tech stack choices | Permanent                             |
| **Episodes**   | Bugs found, incidents, debugging sessions  | Medium-term (compacted after 30 days) |
| **Procedures** | Coding rules, conventions, how-to guides   | Permanent                             |

Memories are stored under `~/.memorykit/<project-name>/` — isolated per project via automatic git root detection. No configuration required for basic use.

---

## Quick Start

### 1. Install

```bash
npm install -g memorykit-mcp-server
```

### 2. Initialize in your project

```bash
cd /your/project
memorykit init
```

This creates:

- `~/.memorykit/<project-name>/` — Memory storage directory
- `.vscode/mcp.json` — GitHub Copilot MCP server config
- `.mcp.json` — Claude Code MCP server config
- `CLAUDE.md` — Claude Code instructions to use memory proactively
- `.github/copilot-instructions.md` — GitHub Copilot instructions to use memory proactively

The instruction files tell AI models to automatically check memory before starting tasks and save learnings when completing work. This ensures memory is used consistently without manual prompting.

### 3. Configure your AI assistant

**GitHub Copilot in VS Code** — Already configured! `memorykit init` creates `.vscode/mcp.json` and `.github/copilot-instructions.md` automatically. The instructions tell Copilot to check memory before tasks and save learnings after.

**Claude Code in VS Code** — Already configured! `memorykit init` creates `.mcp.json` and `CLAUDE.md` automatically. The instructions tell Claude to check memory before tasks and save learnings after.

**Claude Desktop** — Edit the config file:

| OS      | Path                                                              |
| ------- | ----------------------------------------------------------------- |
| Windows | `%APPDATA%\Claude\claude_desktop_config.json`                     |
| macOS   | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| Linux   | `~/.config/Claude/claude_desktop_config.json`                     |

```json
{
  "mcpServers": {
    "memorykit": {
      "command": "memorykit",
      "env": {
        "MEMORYKIT_PROJECT": "/absolute/path/to/your/project"
      }
    }
  }
}
```

**Cursor** — Add to Cursor MCP settings using the same format as Claude Desktop.

### 4. Restart your AI assistant

The 7 MemoryKit tools will appear in the tool list:

- `initialize_memory` — Create memory storage structure (run once per project)
- `store_memory` — Save new memories
- `retrieve_context` — Query relevant memories
- `update_memory` — Modify existing entries
- `forget_memory` — Delete entries
- `list_memories` — Browse stored memories
- `consolidate` — Manual cleanup/optimization (auto-runs every 5 minutes)

---

## Available Tools

### `store_memory`

Save a new memory entry. Importance is scored automatically (0.1–0.95, never absolute 0 or 1) and the correct layer is selected based on content type.

| Parameter             | Type     | Required | Description                                                             |
| --------------------- | -------- | -------- | ----------------------------------------------------------------------- |
| `content`             | string   | ✅       | The memory content                                                      |
| `tags`                | string[] | ❌       | Categorization tags (auto-detected if omitted)                          |
| `layer`               | enum     | ❌       | `working`, `facts`, `episodes`, `procedures` (auto-detected if omitted) |
| `scope`               | enum     | ❌       | `project` (default) or `global`                                         |
| `file_hint`           | string   | ❌       | Target filename within layer (e.g. `"technology"`)                      |
| `acquisition_context` | object   | ❌       | ROI tracking: `{ tokens_consumed: number, tool_calls: number }`         |

**Example:**

```json
{
  "content": "We decided to use PostgreSQL as the primary database because of ACID guarantees and existing team expertise.",
  "tags": ["database", "architecture"],
  "acquisition_context": { "tokens_consumed": 1200, "tool_calls": 3 }
}
```

---

### `retrieve_context`

Get relevant memory context for a query. The Prefrontal Controller classifies your query and routes to the appropriate memory layers automatically.

| Parameter    | Type     | Required | Description                                                      |
| ------------ | -------- | -------- | ---------------------------------------------------------------- |
| `query`      | string   | ✅       | Natural language question or topic                               |
| `max_tokens` | number   | ❌       | Token budget override (default: query-type based — 200 to 2,000) |
| `layers`     | string[] | ❌       | Restrict to specific layers                                      |
| `scope`      | enum     | ❌       | `all` (default), `project`, or `global`                          |

**Example:**

```json
{
  "query": "what database are we using and why?",
  "scope": "all"
}
```

---

### `update_memory`

Modify an existing memory entry by ID.

| Parameter    | Type     | Required | Description                           |
| ------------ | -------- | -------- | ------------------------------------- |
| `entry_id`   | string   | ✅       | Entry ID to update                    |
| `content`    | string   | ❌       | New content                           |
| `tags`       | string[] | ❌       | Updated tags                          |
| `importance` | number   | ❌       | Manual importance override (0.1–0.95) |

---

### `forget_memory`

Delete a memory entry by ID.

| Parameter  | Type   | Required | Description        |
| ---------- | ------ | -------- | ------------------ |
| `entry_id` | string | ✅       | Entry ID to delete |

---

### `consolidate`

Run memory maintenance: prune stale working memory, promote high-importance entries to long-term layers, and compact old episode files.

| Parameter | Type    | Required | Description                             |
| --------- | ------- | -------- | --------------------------------------- |
| `scope`   | enum    | ❌       | `project` (default), `global`, or `all` |
| `dry_run` | boolean | ❌       | Preview changes without modifying files |

---

### `list_memories`

Browse the memory structure and see entry counts per layer.

| Parameter | Type | Required | Description                             |
| --------- | ---- | -------- | --------------------------------------- |
| `scope`   | enum | ❌       | `all` (default), `project`, or `global` |
| `layer`   | enum | ❌       | Filter to a specific layer              |

---

## Memory Quality Gates

MemoryKit filters low-quality entries automatically before storing:

1. **Importance Floor** — Content scoring below 0.15 is rejected as routine or trivial.
2. **Duplicate Detection** — Near-duplicate entries are blocked; the existing entry ID is returned so you can update it instead.
3. **Contradiction Warning** — If new content potentially conflicts with existing knowledge, storage proceeds but a warning is returned.

---

## CLI Commands

```bash
# Initialize memory for the current project
memorykit init

# Initialize global memory shared across all projects
memorykit init --global

# Show memory statistics (entry counts, file sizes, last consolidation)
memorykit status

# Run memory consolidation (prune stale, promote important, compact old episodes)
memorykit consolidate

# Preview consolidation without making changes
memorykit consolidate --dry-run
```

---

## Configuration

After `memorykit init`, a `memorykit.yaml` is created at `~/.memorykit/<project-name>/memorykit.yaml`:

```yaml
version: "0.1"

working:
  max_entries: 50
  decay_threshold_days: 7
  promotion_threshold: 0.70

facts:
  max_entries_per_file: 100

episodes:
  compaction_after_days: 30

consolidation:
  auto: true
  interval_minutes: 0

global:
  enabled: true
  priority: "project"

context:
  max_tokens_estimate: 4000

quality_gates:
  importance_floor: 0.15
  duplicate_jaccard_threshold: 0.6
  duplicate_word_overlap: 3
```

---

## Storage Layout

```
~/.memorykit/
├── <project-name>/
│   ├── memorykit.yaml
│   ├── working/
│   │   └── session.md
│   ├── facts/
│   │   ├── architecture.md
│   │   ├── technology.md
│   │   └── general.md
│   ├── episodes/
│   │   └── 2026-03-04.md
│   └── procedures/
│       └── general.md
└── facts/                       # Global memory (shared across all projects)
    └── ...
```

---

## Architecture

```
AI Assistant (Claude / Copilot / Cursor)
     │  MCP Protocol (stdio)
     ↓
MemoryKit MCP Server (Node.js)
     ├── Prefrontal Controller   Query classification & file routing
     ├── Amygdala Engine         Importance scoring (9-signal, 0.1–0.95)
     ├── Quality Gates           Importance floor, duplicate detection, contradiction warning
     ├── Normalizer              Prose-to-MML normalization pipeline
     └── File Storage            Local Markdown files (~/.memorykit/)
```

**Query Classification** (Prefrontal): Routes `retrieve_context` queries to the right files:

- _Continuation_ → `working/session.md`
- _Fact retrieval_ → `facts/*.md`
- _Deep recall_ → `episodes/*.md`
- _Procedural_ → `procedures/*.md`
- _Complex_ → all layers

**Importance Scoring** (Amygdala): 9 signals scored 0.1–0.95 — decision language, explicit importance markers, code blocks, technical depth, novelty, sentiment, conversation context, question patterns, and MML structure.

---

## Environment Variables

| Variable            | Description                   | Default                     |
| ------------------- | ----------------------------- | --------------------------- |
| `MEMORYKIT_PROJECT` | Absolute path to project root | Auto-detected from git root |

---

## Requirements

- Node.js 18+
- No database required
- No Docker required
- No API keys required

---

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

MIT — see [LICENSE](LICENSE) for details.

## Support

- Issues: [GitHub Issues](https://github.com/antonio-rapozo/memorykit/issues)
- Discussions: [GitHub Discussions](https://github.com/antonio-rapozo/memorykit/discussions)
