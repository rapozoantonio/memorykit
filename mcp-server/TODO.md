Summarized conversation historyI'll analyze this PRD and create a comprehensive implementation plan. Let me first verify the current codebase state against the PRD requirements.

Search complete: Verify MCP server tools

Searched for files matching `**/mcp-server/src/tools/**/*.ts`, 1 match

Searched for regex `store_memory|retrieve_context`, 10 results

Read [](file:///c%3A/Users/rapoz/Documents/web-dev/memorykit/mcp-server/src/tools/index.ts)

Read [](file:///c%3A/Users/rapoz/Documents/web-dev/memorykit/mcp-server/src/api-client.ts)

Searched for files matching `**/mcp-server/src/**/*.ts`, 6 matches

Read [](file:///c%3A/Users/rapoz/Documents/web-dev/memorykit/src/MemoryKit.Application/Services/AmygdalaImportanceEngine.cs#1-1), lines 1 to 150

Read [](file:///c%3A/Users/rapoz/Documents/web-dev/memorykit/src/MemoryKit.Application/Services/PrefrontalController.cs#1-1), lines 1 to 150

Read [](file:///c%3A/Users/rapoz/Documents/web-dev/memorykit/mcp-server/src)

Read [](file:///c%3A/Users/rapoz/Documents/web-dev/memorykit/mcp-server/package.json)

Searched for regex `DecisionPatterns|ImportanceMarkers|ContinuationPatterns|FactRetrievalPhrases` (`**/*.cs`), 20 results

Read [](file:///c%3A/Users/rapoz/Documents/web-dev/memorykit/src/MemoryKit.Application/Services/AmygdalaImportanceEngine.cs#400-400), lines 400 to 490

Read [](file:///c%3A/Users/rapoz/Documents/web-dev/memorykit/src/MemoryKit.Application/Services/PrefrontalController.cs#550-550), lines 550 to 690

Now I have enough context to create the implementation plan.

---

# MemoryKit Implementation Plan

## 0. Verification Results

✅ **Verified:**

- Current MCP server at index.ts — API-based, 5 tools
- AmygdalaImportanceEngine.cs confirmed at AmygdalaImportanceEngine.cs (490 lines)
- PrefrontalController.cs confirmed at PrefrontalController.cs (690 lines)
- Pattern definitions verified: DecisionPatterns, ImportanceMarkers, ContinuationPatterns, FactRetrievalPhrases, DeepRecallPatterns, ProceduralTriggerTokens
- Package.json exists at package.json — currently `@memorykit/mcp-server`

⚠️ **Concerns:**

- No file-based storage exists — must be built from scratch
- No entry parser for blockquote metadata format — new implementation
- No CLI infrastructure — new implementation
- Current tools use `conversation_id` model — PRD removes this concept entirely

**Ready to proceed:** Yes

---

## 1. Overview

**What's being built:** A complete rewrite of the MCP server from API-based to file-based storage with cognitive intelligence ported from C#.

**Primary goal:** Zero-infrastructure memory for AI coding assistants via local markdown files.

**Success criteria:**

- `npx memorykit init` creates `.memorykit/` structure
- 6 MCP tools functional without any backend API
- Amygdala scores importance, Prefrontal classifies queries
- Merges global (`~/.memorykit/`) and project (`.memorykit/`) scopes

**Impact scope:**

- Complete rewrite of mcp-server directory
- New package identity (`memorykit` not `@memorykit/mcp-server`)
- C# codebase unaffected (MemoryKit Enterprise)

---

## 2. Requirements Analysis

### Functional Requirements

| Requirement | Description                                                                   |
| ----------- | ----------------------------------------------------------------------------- |
| FR-1        | `store_memory` — Write entries with auto importance scoring and layer routing |
| FR-2        | `retrieve_context` — Read relevant memory based on query classification       |
| FR-3        | `update_memory` — Modify existing entries by ID                               |
| FR-4        | `forget_memory` — Remove entries                                              |
| FR-5        | `consolidate` — Prune, promote, compact memory                                |
| FR-6        | `list_memories` — Browse memory structure                                     |
| FR-7        | CLI `init` command creates `.memorykit/` folder structure                     |
| FR-8        | CLI `status` command shows memory statistics                                  |
| FR-9        | Merge global + project scope with project priority                            |
| FR-10       | Auto-consolidation on store (configurable)                                    |

### Non-Functional Requirements

| Requirement | Description                                       |
| ----------- | ------------------------------------------------- | ----------- |
| NFR-1       | Parse blockquote metadata format: `> key: value   | key: value` |
| NFR-2       | Generate entry IDs: `e_{timestamp}_{4_char_hash}` |
| NFR-3       | Token estimation (~3.5 chars/token)               |
| NFR-4       | Files are human-readable markdown                 |
| NFR-5       | Git-friendly (trackable, mergeable)               |

### Acceptance Criteria

1. `npx memorykit init` creates valid folder structure in <2 seconds
2. `store_memory` with no `layer` param auto-routes correctly >90% of cases
3. `retrieve_context` returns relevant entries within token budget
4. All memory files pass markdown lint
5. Entry metadata parses correctly round-trip (write → read → write)

---

## 3. Technical Context

### Files to Create (New)

```
mcp-server/
├── src/
│   ├── index.ts                    # NEW: Entry point (rewrite)
│   ├── server.ts                   # NEW: MCP server setup
│   ├── cli.ts                      # NEW: CLI commands
│   ├── cognitive/
│   │   ├── amygdala.ts             # NEW: Port from C# (490 lines)
│   │   ├── prefrontal.ts           # NEW: Port from C# (690 lines)
│   │   └── patterns.ts             # NEW: Shared regex constants
│   ├── memory/
│   │   ├── store.ts                # NEW: Write operations
│   │   ├── retrieve.ts             # NEW: Read operations
│   │   ├── update.ts               # NEW: Update operations
│   │   ├── forget.ts               # NEW: Delete operations
│   │   └── consolidate.ts          # NEW: Maintenance operations
│   ├── storage/
│   │   ├── file-manager.ts         # NEW: File I/O
│   │   ├── entry-parser.ts         # NEW: Metadata parsing
│   │   ├── scope-resolver.ts       # NEW: Path resolution
│   │   └── config-loader.ts        # NEW: YAML config parsing
│   ├── tools/
│   │   ├── store-memory.ts         # NEW: Tool implementation
│   │   ├── retrieve-context.ts     # NEW: Tool implementation
│   │   ├── update-memory.ts        # NEW: Tool implementation
│   │   ├── forget-memory.ts        # NEW: Tool implementation
│   │   ├── consolidate.ts          # NEW: Tool implementation
│   │   └── list-memories.ts        # NEW: Tool implementation
│   └── types/
│       ├── memory.ts               # NEW: Type definitions
│       ├── cognitive.ts            # NEW: Type definitions
│       └── config.ts               # NEW: Type definitions
├── templates/
│   ├── memorykit.yaml              # NEW: Default config template
│   └── session.md                  # NEW: Default working memory file
└── tests/
    ├── cognitive/
    ├── memory/
    └── storage/
```

### Files to Remove/Deprecate

- api-client.ts — No longer needed
- process-manager.ts — No longer needed
- process-manager-dev.ts — No longer needed
- index-dev.ts — No longer needed
- index.ts — Complete rewrite

### Patterns to Follow from C#

**From AmygdalaImportanceEngine.cs:**

- Geometric mean scoring algorithm
- Signal components: decision, explicit, question, code, novelty, sentiment, technical, context
- Pattern arrays: DecisionPatterns, ImportanceMarkers, PositiveMarkers, NegativeMarkers

**From PrefrontalController.cs:**

- QuickClassify fast-path pattern matching
- Signal-based classification fallback
- Layer routing based on QueryType

### Dependencies to Add

```json
{
  "dependencies": {
    "@modelcontextprotocol/sdk": "^1.0.0",
    "yaml": "^2.3.0",
    "commander": "^11.0.0"
  },
  "devDependencies": {
    "vitest": "^1.0.0"
  }
}
```

Remove: `axios` (no HTTP calls)

---

## 4. Implementation Steps

### Phase 1: Foundation (Types + Storage Layer)

#### Step 1.1 — Type Definitions

**[File: `mcp-server/src/types/memory.ts`]**

- Define `MemoryEntry` interface (id, content, importance, created, tags, source, etc.)
- Define `MemoryLayer` enum: `working`, `facts`, `episodes`, `procedures`
- Define `MemoryScope` enum: `project`, `global`
- Define `MemoryFile` interface (layer, filename, entries)

**[File: `mcp-server/src/types/cognitive.ts`]**

- Define `ImportanceSignals` interface (9 signal components)
- Define `QueryClassification` interface (type, confidence)
- Define `QueryType` enum: `continuation`, `factRetrieval`, `deepRecall`, `procedural`, `complex`, `store`

**[File: `mcp-server/src/types/config.ts`]**

- Define `MemoryKitConfig` interface matching `memorykit.yaml` schema
- Include working, facts, episodes, procedures, consolidation, global, context sections

---

#### Step 1.2 — Entry Parser

**[File: `mcp-server/src/storage/entry-parser.ts`]**

- `parseEntry(rawText: string): MemoryEntry` — Parse blockquote metadata + content
- `serializeEntry(entry: MemoryEntry): string` — Write entry back to markdown format
- `parseMetadataLine(line: string): Record<string, string>` — Parse `> key: val | key: val`
- `generateEntryId(content: string): string` — Generate `e_{timestamp}_{hash}`

Metadata format:

```
> importance: 0.85 | created: 2026-02-16T10:30:00Z | tags: database, architecture | source: conversation
```

---

#### Step 1.3 — File Manager

**[File: `mcp-server/src/storage/file-manager.ts`]**

- `readMemoryFile(filePath: string): MemoryEntry[]` — Parse file into entries
- `writeMemoryFile(filePath: string, entries: MemoryEntry[]): void` — Write entries
- `appendEntry(filePath: string, entry: MemoryEntry): void` — Add single entry
- `removeEntry(filePath: string, entryId: string): boolean` — Remove by ID
- `updateEntry(filePath: string, entryId: string, updates: Partial<MemoryEntry>): boolean`
- `ensureDirectoryExists(dirPath: string): void` — Create dirs as needed
- `listMemoryFiles(rootPath: string): FileInfo[]` — List all .md files with stats

---

#### Step 1.4 — Scope Resolver

**[File: `mcp-server/src/storage/scope-resolver.ts`]**

- `resolveProjectRoot(): string` — Find `.memorykit/` in cwd or parents
- `resolveGlobalRoot(): string` — Return `~/.memorykit/`
- `resolveLayerPath(scope: Scope, layer: Layer): string` — Build full path
- `resolveFilePath(scope: Scope, layer: Layer, filename: string): string`
- `getWorkingDirectory(): string` — Handle `MEMORYKIT_PROJECT` env override

---

#### Step 1.5 — Config Loader

**[File: `mcp-server/src/storage/config-loader.ts`]**

- `loadConfig(rootPath: string): MemoryKitConfig` — Parse `memorykit.yaml`
- `mergeConfigs(project: Config, global: Config): Config` — Merge with project priority
- `getDefaultConfig(): MemoryKitConfig` — Return default values
- `validateConfig(config: unknown): MemoryKitConfig` — Validate with Zod schema

---

### Phase 2: Cognitive Layer (Port from C#)

#### Step 2.1 — Shared Patterns

**[File: `mcp-server/src/cognitive/patterns.ts`]**

Port all pattern constants from C#:

```typescript
export const DecisionPatterns = [
  "i will",
  "let's",
  "we should",
  "i decided",
  "going to",
  "plan to",
  "commit to",
  "i'll",
  "we'll",
  "must",
];

export const ImportanceMarkers = [
  "important",
  "critical",
  "remember",
  "don't forget",
  "note that",
  "always",
  "never",
  "from now on",
  "crucial",
  "essential",
  "key point",
  "take note",
];

export const ContinuationPatterns = [
  "continue",
  "go on",
  "and then",
  "next",
  "keep going",
  "more",
];

export const FactRetrievalPhrases = [
  "what was",
  "what is",
  "who is",
  "when did",
  "how many",
  "tell me about",
  "remind me",
];

export const DeepRecallPatterns = [
  "quote",
  "exactly",
  "verbatim",
  "word for word",
  "precise",
  "show me the",
  "find the conversation",
];

export const ProceduralTriggerTokens = new Set([
  "create",
  "generate",
  "build",
  "implement",
  "format",
  "structure",
  "write",
]);

// Weighted patterns for scoring
export const DecisionPatternsWeighted: [string, number][] = [
  ["decided", 0.5],
  ["committed", 0.5],
  ["final decision", 0.5],
  ["will ", 0.25],
  ["going to", 0.25],
  ["plan to", 0.25],
  ["consider", 0.15],
  ["thinking about", 0.15],
  ["maybe", 0.15],
];

export const ImportanceMarkersWeighted: [string, number][] = [
  ["critical", 0.6],
  ["crucial", 0.6],
  ["essential", 0.6],
  ["must", 0.6],
  ["required", 0.6],
  ["vital", 0.6],
  ["important", 0.4],
  ["remember", 0.4],
  ["note that", 0.4],
];
```

---

#### Step 2.2 — Amygdala Importance Engine

**[File: `mcp-server/src/cognitive/amygdala.ts`]**

Port from AmygdalaImportanceEngine.cs (~300 lines after TypeScript conversion):

```typescript
interface ImportanceSignals {
    decisionLanguage: number;
    explicitImportance: number;
    question: number;
    codeBlocks: number;
    novelty: number;
    sentiment: number;
    technicalDepth: number;
    conversationContext: number;
}

export function calculateImportance(content: string, context?: EntryContext): number {
    const signals = calculateAllSignals(content, context);
    return computeGeometricMean(signals);
}

// Individual signal detectors
function detectDecisionLanguage(content: string): number { ... }
function detectExplicitImportance(content: string): number { ... }
function detectQuestion(content: string): number { ... }
function detectCodeBlocks(content: string): number { ... }
function detectNovelty(content: string, context?: EntryContext): number { ... }
function detectSentiment(content: string): number { ... }
function detectTechnicalDepth(content: string): number { ... }
function detectConversationContext(content: string): number { ... }

function computeGeometricMean(signals: ImportanceSignals): number {
    const values = Object.values(signals).filter(s => s > 0.01);
    if (values.length === 0) return 0.1;
    const product = values.reduce((a, b) => a * b, 1);
    const mean = Math.pow(product, 1 / values.length);
    return Math.max(0.05, Math.min(0.95, mean * 0.90));
}
```

---

#### Step 2.3 — Prefrontal Controller

**[File: `mcp-server/src/cognitive/prefrontal.ts`]**

Port from PrefrontalController.cs (~400 lines after TypeScript conversion):

```typescript
interface QueryClassification {
    type: QueryType;
    confidence: number;
}

interface FileSet {
    project: string[];
    global: string[];
}

export function classifyQuery(query: string): QueryClassification {
    // Fast-path pattern matching (handles ~80%)
    const quick = quickClassify(query);
    if (quick) return quick;

    // Signal-based classification
    const signals = calculateQuerySignals(query);
    return classifyBySignals(signals);
}

export function resolveFiles(classification: QueryClassification, config: Config): FileSet {
    switch (classification.type) {
        case 'continuation': return { project: ['working/session.md'], global: [] };
        case 'factRetrieval': return { project: ['facts/*.md', 'working/session.md'], global: ['facts/*.md'] };
        case 'deepRecall': return { project: ['episodes/*.md', 'facts/*.md'], global: [] };
        case 'procedural': return { project: ['procedures/*.md'], global: ['procedures/*.md'] };
        case 'complex': return { project: ['facts/*.md', 'working/session.md', 'procedures/*.md'], global: ['facts/*.md', 'procedures/*.md'] };
    }
}

function quickClassify(query: string): QueryClassification | null { ... }
function calculateQuerySignals(query: string): QuerySignals { ... }
function classifyBySignals(signals: QuerySignals): QueryClassification { ... }
```

---

### Phase 3: Memory Operations

#### Step 3.1 — Store Operation

**[File: `mcp-server/src/memory/store.ts`]**

- `storeMemory(content: string, options?: StoreOptions): StoreResult`
- Auto-calculate importance via Amygdala
- Determine layer via Prefrontal if not specified
- Determine target file based on tags/content
- Append entry to file
- Trigger consolidation check if auto-consolidation enabled

---

#### Step 3.2 — Retrieve Operation

**[File: `mcp-server/src/memory/retrieve.ts`]**

- `retrieveContext(query: string, options?: RetrieveOptions): RetrieveResult`
- Classify query via Prefrontal
- Resolve files to read from both scopes
- Read and parse all relevant entries
- Sort by `importance × recency_factor`
- Merge project + global (project priority)
- Truncate to token budget
- Format as markdown

---

#### Step 3.3 — Update Operation

**[File: `mcp-server/src/memory/update.ts`]**

- `updateMemory(entryId: string, updates: UpdateOptions): UpdateResult`
- Search all files for entry by ID
- Apply updates (content, tags, importance override)
- Re-score importance if content changed (unless overridden)
- Write back to file

---

#### Step 3.4 — Forget Operation

**[File: `mcp-server/src/memory/forget.ts`]**

- `forgetMemory(entryId: string): ForgetResult`
- Search all files for entry by ID
- Remove entry from file
- Clean up empty files (delete if only entry)

---

#### Step 3.5 — Consolidate Operation

**[File: `mcp-server/src/memory/consolidate.ts`]**

- `consolidateMemory(scope: Scope, options?: ConsolidateOptions): ConsolidateResult`
- **Rule 1:** Prune old, low-importance working memory entries
- **Rule 2:** Promote high-importance working entries to facts/episodes/procedures
- **Rule 3:** Compact old episode entries (truncate content)
- **Rule 4:** Enforce working memory size cap
- Return detailed action log

---

### Phase 4: MCP Tool Implementations

#### Step 4.1 — Tool: store_memory

**[File: `mcp-server/src/tools/store-memory.ts`]**

```typescript
export const storeMemoryTool = {
    name: "store_memory",
    description: "Store a new memory entry with automatic importance scoring and layer routing",
    inputSchema: {
        type: "object",
        properties: {
            content: { type: "string", description: "The memory content to store" },
            tags: { type: "array", items: { type: "string" }, description: "Categorization tags" },
            layer: { type: "string", enum: ["working", "facts", "episodes", "procedures"], description: "Override layer" },
            scope: { type: "string", enum: ["project", "global"], default: "project" },
            file_hint: { type: "string", description: "Target file within layer (e.g., 'technology')" }
        },
        required: ["content"]
    },
    handler: async (args: StoreMemoryArgs): Promise<StoreResult> => { ... }
};
```

---

#### Step 4.2 — Tool: retrieve_context

**[File: `mcp-server/src/tools/retrieve-context.ts`]**

```typescript
export const retrieveContextTool = {
    name: "retrieve_context",
    description: "Get relevant memory context for a query with intelligent routing",
    inputSchema: {
        type: "object",
        properties: {
            query: { type: "string", description: "The question or topic to retrieve context for" },
            max_tokens: { type: "number", description: "Token budget override" },
            layers: { type: "array", items: { type: "string" }, description: "Restrict to specific layers" },
            scope: { type: "string", enum: ["all", "project", "global"], default: "all" }
        },
        required: ["query"]
    },
    handler: async (args: RetrieveContextArgs): Promise<RetrieveResult> => { ... }
};
```

---

#### Step 4.3 — Tool: update_memory

**[File: `mcp-server/src/tools/update-memory.ts`]**

```typescript
export const updateMemoryTool = {
    name: "update_memory",
    description: "Modify an existing memory entry",
    inputSchema: {
        type: "object",
        properties: {
            entry_id: { type: "string", description: "ID of entry to update" },
            content: { type: "string", description: "New content (replaces existing)" },
            tags: { type: "array", items: { type: "string" } },
            importance: { type: "number", description: "Manual importance override" }
        },
        required: ["entry_id"]
    },
    handler: async (args: UpdateMemoryArgs): Promise<UpdateResult> => { ... }
};
```

---

#### Step 4.4 — Tool: forget_memory

**[File: `mcp-server/src/tools/forget-memory.ts`]**

```typescript
export const forgetMemoryTool = {
    name: "forget_memory",
    description: "Remove a memory entry",
    inputSchema: {
        type: "object",
        properties: {
            entry_id: { type: "string", description: "ID of entry to remove" }
        },
        required: ["entry_id"]
    },
    handler: async (args: ForgetMemoryArgs): Promise<ForgetResult> => { ... }
};
```

---

#### Step 4.5 — Tool: consolidate

**[File: `mcp-server/src/tools/consolidate.ts`]**

```typescript
export const consolidateTool = {
    name: "consolidate",
    description: "Trigger memory maintenance (prune, promote, compact)",
    inputSchema: {
        type: "object",
        properties: {
            scope: { type: "string", enum: ["project", "global", "all"], default: "project" },
            dry_run: { type: "boolean", description: "Report changes without modifying files" }
        }
    },
    handler: async (args: ConsolidateArgs): Promise<ConsolidateResult> => { ... }
};
```

---

#### Step 4.6 — Tool: list_memories

**[File: `mcp-server/src/tools/list-memories.ts`]**

```typescript
export const listMemoriesTool = {
    name: "list_memories",
    description: "Browse memory structure and statistics",
    inputSchema: {
        type: "object",
        properties: {
            scope: { type: "string", enum: ["all", "project", "global"], default: "all" },
            layer: { type: "string", enum: ["working", "facts", "episodes", "procedures"] }
        }
    },
    handler: async (args: ListMemoriesArgs): Promise<ListResult> => { ... }
};
```

---

### Phase 5: MCP Server + CLI

#### Step 5.1 — MCP Server Setup

**[File: `mcp-server/src/server.ts`]**

- Initialize MCP server with `@modelcontextprotocol/sdk`
- Register all 6 tools
- Handle ListToolsRequest
- Handle CallToolRequest with routing to tool handlers

**[File: `mcp-server/src/index.ts`]**

- Entry point: parse args
- If `init` or `status` or `consolidate` → run CLI command
- Else → start MCP server (stdio mode)

---

#### Step 5.2 — CLI Commands

**[File: `mcp-server/src/cli.ts`]**

```typescript
import { Command } from 'commander';

const program = new Command();

program
    .name('memorykit')
    .description('Cognitive memory for AI coding assistants')
    .version('0.1.0');

program
    .command('init')
    .description('Initialize .memorykit/ directory')
    .option('--global', 'Initialize global memory (~/.memorykit/)')
    .action(initCommand);

program
    .command('status')
    .description('Show memory statistics')
    .action(statusCommand);

program
    .command('consolidate')
    .description('Run memory maintenance')
    .option('--scope <scope>', 'project, global, or all', 'project')
    .option('--dry-run', 'Report without modifying')
    .action(consolidateCommand);

async function initCommand(options: InitOptions) { ... }
async function statusCommand() { ... }
async function consolidateCommand(options: ConsolidateOptions) { ... }
```

---

#### Step 5.3 — Templates

**[File: `mcp-server/templates/memorykit.yaml`]**

```yaml
version: "0.1"

working:
  max_entries: 50
  decay_threshold_days: 7
  promotion_threshold: 0.70

facts:
  max_entries_per_file: 100
  auto_categorize: true

episodes:
  date_format: "YYYY-MM-DD"
  compaction_after_days: 30

procedures:
  trigger_patterns: true

consolidation:
  auto: true
  interval_minutes: 0

global:
  enabled: true
  priority: "project"

context:
  max_tokens_estimate: 4000
  prioritize_by: "importance"
```

**[File: `mcp-server/templates/session.md`]**

```markdown
# Working Memory

Current session context and active tasks.

---
```

---

### Phase 6: Package Configuration

#### Step 6.1 — Package.json Update

**[File: package.json]**

```json
{
  "name": "memorykit",
  "version": "0.1.0",
  "description": "Cognitive memory for AI coding assistants",
  "type": "module",
  "main": "./dist/index.js",
  "bin": {
    "memorykit": "./dist/index.js"
  },
  "scripts": {
    "build": "tsc",
    "dev": "tsx src/index.ts",
    "start": "node dist/index.js",
    "test": "vitest",
    "prepare": "npm run build"
  },
  "keywords": [
    "mcp",
    "memory",
    "ai",
    "cursor",
    "copilot",
    "llm",
    "context",
    "cognitive"
  ],
  "dependencies": {
    "@modelcontextprotocol/sdk": "^1.0.0",
    "yaml": "^2.3.0",
    "commander": "^11.0.0",
    "zod": "^3.22.0"
  },
  "devDependencies": {
    "@types/node": "^20.0.0",
    "tsx": "^4.7.0",
    "typescript": "^5.3.0",
    "vitest": "^1.0.0"
  }
}
```

---

## 5. Testing Strategy

### Unit Tests

| Component      | Test File                              | Coverage                                         |
| -------------- | -------------------------------------- | ------------------------------------------------ |
| Entry Parser   | `tests/storage/entry-parser.test.ts`   | Parse/serialize round-trip, metadata extraction  |
| Amygdala       | `tests/cognitive/amygdala.test.ts`     | Each signal detector, geometric mean calculation |
| Prefrontal     | `tests/cognitive/prefrontal.test.ts`   | quickClassify patterns, signal classification    |
| File Manager   | `tests/storage/file-manager.test.ts`   | Read/write/append/remove operations              |
| Scope Resolver | `tests/storage/scope-resolver.test.ts` | Path resolution, env override                    |

### Integration Tests

| Scenario                    | Description                                      |
| --------------------------- | ------------------------------------------------ |
| Init creates structure      | `memorykit init` creates all folders and files   |
| Store → Retrieve round-trip | Store entry, retrieve by query, verify content   |
| Consolidation rules         | Verify prune/promote/compact behaviors           |
| Scope merge                 | Project entry overrides global entry on conflict |

### Manual Testing

1. Run `npx memorykit init` in test project
2. Configure MCP in Cursor/VS Code
3. Have AI store memories via conversation
4. Verify `.memorykit/` files are readable markdown
5. Have AI retrieve context — verify relevance

---

## 6. Potential Risks & Trade-offs

### 🔴 High Priority Risks

| Risk                                 | Mitigation                                                              |
| ------------------------------------ | ----------------------------------------------------------------------- |
| Entry ID collisions                  | Use timestamp + content hash (4 chars unique enough for single project) |
| File corruption on concurrent writes | Node.js single-threaded; MCP calls sequential. No mutex needed.         |
| Token estimation drift               | Use conservative 3.5 chars/token; actual varies by model                |

### 🟡 Medium Priority Considerations

| Consideration                         | Trade-off                                                                     |
| ------------------------------------- | ----------------------------------------------------------------------------- |
| No semantic search                    | LLM reads files directly — relies on LLM comprehension, not vector similarity |
| Large memory folders                  | Token budget limits context; consolidation prunes old entries                 |
| Pattern-based classification accuracy | ~80% quick classify; fallback handles edge cases                              |

### 🟢 Low Priority Notes

| Note                           | Impact                                            |
| ------------------------------ | ------------------------------------------------- |
| No encryption at rest          | Local files; user's filesystem security applies   |
| Markdown formatting edge cases | Test with code blocks containing `---` separators |
| Global memory conflicts        | Project always wins; no sophisticated merge       |

### Security Considerations

- ✅ All files local — no network calls
- ✅ No credentials stored
- ⚠️ Memory files may contain sensitive project info — recommend .gitignore for `working/`

### Performance Impact

- File I/O on every store/retrieve — acceptable for single-developer use
- Consolidation inline (default) adds ~10-50ms to store
- Token estimation is O(1) string length check

---

## 7. Estimated Effort

| Phase                      | Complexity | Time Estimate |
| -------------------------- | ---------- | ------------- |
| Phase 1: Foundation        | Low        | 4-6 hours     |
| Phase 2: Cognitive Layer   | Medium     | 8-12 hours    |
| Phase 3: Memory Operations | Medium     | 4-6 hours     |
| Phase 4: MCP Tools         | Low        | 3-4 hours     |
| Phase 5: Server + CLI      | Low        | 2-3 hours     |
| Phase 6: Package Config    | Low        | 1 hour        |
| Testing                    | Medium     | 4-6 hours     |
| Documentation              | Low        | 2-3 hours     |

**Total: 28-41 hours (~4-5 days)**

**Complexity Rating:** Medium

**Skills Required:**

- TypeScript (intermediate)
- MCP protocol understanding
- Regex pattern matching
- File system operations

**Blockers:** None — all dependencies are stable npm packages

---

## 8. Deployment Considerations

### npm Publishing

```bash
npm login
npm publish --access public
```

### Package Verification

1. `npx memorykit@latest init` works globally
2. `npm install memorykit` installs without errors
3. Binary `memorykit` available in PATH after install

### Documentation Updates

- Update README.md with new architecture
- Update root README.md to differentiate MemoryKit vs Enterprise
- Add `MIGRATION.md` for users of API-based version

### Rollback Plan

- Keep old API-based code in `mcp-server-enterprise/` branch
- Tag current state before rewrite: `git tag pre-file-based`
- npm unpublish within 72 hours if critical issues

### Monitoring

- GitHub issues for bug reports
- npm download stats
- User feedback via Cursor/VS Code marketplace listings

---

## File Order Summary

**Implementation order (dependency-driven):**

1. `types/memory.ts`, `types/cognitive.ts`, `types/config.ts`
2. `cognitive/patterns.ts`
3. `storage/entry-parser.ts`
4. `storage/file-manager.ts`
5. `storage/scope-resolver.ts`
6. `storage/config-loader.ts`
7. `cognitive/amygdala.ts`
8. `cognitive/prefrontal.ts`
9. `memory/store.ts`, `memory/retrieve.ts`, `memory/update.ts`, `memory/forget.ts`, `memory/consolidate.ts`
10. `tools/*.ts` (all 6 tools)
11. `server.ts`
12. `cli.ts`
13. `index.ts`
14. `templates/*`
15. `tests/*`

---

**Token estimate:** ~3,500 words
**Task status:** Plan complete, ready for implementation
