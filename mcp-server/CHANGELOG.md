# Changelog

All notable changes to MemoryKit MCP Server are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.4.3] — 2026-08-09

### Fixed

- **`memorykit init` wrote an invalid `statusLine` config** — the generated `~/.claude/settings.json` entry was `{ "command": "memorykit statusline" }`, missing the required `"type": "command"` field. Claude Code's schema rejects the whole file when this field is missing, silently disabling all permissions and settings in `~/.claude/settings.json` for every project. `init` now writes `{ "type": "command", "command": "memorykit statusline" }`, and re-running `init` will detect and repair a previously-broken `statusLine` entry left by older versions.

## [1.4.2] — 2026-07-30

### Fixed

- **MCP Registry rejected the `server.json` submission** — the registry validates the `description` against the *npm-published* `package.json`'s `description` field (not `server.json`'s), which exceeded the registry's 100-character limit. Shortened `package.json`'s `description` to fit; this is also now the description shown on the npm listing page.

## [1.4.1] — 2026-07-30

### Added

- **MCP Registry metadata** — added `mcpName` to `package.json` and a `server.json` manifest so the server can be published to the official [MCP Registry](https://registry.modelcontextprotocol.io), making it discoverable outside of npm search.

## [1.4.0] — 2026-07-30

### Fixed

- **Embeddings recomputed on every retrieval** — `MemoryEntry.embedding` is intentionally never written to the human-readable `.md` files, but there was also no cache anywhere else, so every `retrieve_context` call re-ran the local transformer model on every entry with no persisted embedding (i.e. all of them) on every call. Added a sidecar cache (`.embeddings-cache.json` per scope root, keyed by entry id + content hash) so embeddings are computed once and reused.
- **Relevance "noise filter" was a no-op** — `calculateRelevance()` floored token-overlap relevance at `0.1`, and the noise-filter threshold was also `0.1`, so a zero-overlap entry always cleared the filter (`0.1 >= 0.1`). Zero-overlap entries now score `0` and are correctly excluded; the floor of `0.1` still applies once there's at least a partial match, so a single weak match isn't scored disproportionately low.
- **Short technical queries never searched Facts** — `classifyQuery()` treated any query under 5 words without a `?` as a bare conversational continuation, restricting retrieval to Working memory only. Single-word queries like `"database"` or `"auth"` never reached the Facts layer at all. Short queries containing a known technical term now classify as `FactRetrieval`.
- **`onnxruntime-node` version mismatch** broke local embedding generation entirely in dev/test — `@xenova/transformers` expects `1.14.0`, but a stale lockfile had resolved `1.16.3`, which fails to load with `no available backend found`. Regenerated `package-lock.json` to pin the correct version.
- Relevance filter threshold lowered from `0.1` to `0.05` — weak-but-real semantic matches (topically related content in the `0.05–0.1` cosine range) were being excluded entirely instead of just ranked lower, which could hide a high-importance entry from a loosely-matching query.
- **Auto-pruned entries left orphaned entity-graph edges** — `consolidate()` (which runs automatically every 5 minutes) removes low-importance/over-capacity entries from Working memory, but never called `removeEntryFromGraph`, unlike `forget_memory` which does. Entity graph data (Tier 2) silently accumulated dangling references on every normal consolidation cycle. Both pruning paths now clean up the entity graph.
- **Memory writes weren't crash-safe** — `writeMemoryFile()` wrote directly to the target path with plain `fs.writeFile`; a crash or concurrent external process mid-write could leave a memory file (holding multiple entries) truncated or corrupted. Writes now go to a temp file and `rename` into place, so the original content is untouched until the atomic rename succeeds.
- **Project memory could collide across unrelated repos** — `resolveProjectRoot()` keyed storage by folder basename alone (`~/.memorykit/<name>/`), so two different repos both named e.g. `api` on the same machine would silently share (and pollute) the same memory. The directory name now includes a hash of the absolute project path. Existing installations are migrated automatically on first run — the old folder is renamed into the new path if the new one doesn't already exist; the migration check itself is now cached per process so `resolveProjectRoot()` (called on nearly every tool invocation) doesn't do disk I/O on every single call.
- **Embedding cache had a concurrent-write race** — `embedding-cache.ts`'s `loadCache()` had no dedup for concurrent first-callers on the same scope root, so e.g. backfilling embeddings for multiple entries in one `retrieve_context` call (`Promise.all`) could have each caller read/parse an independent copy of the cache and silently clobber each other's writes. Concurrent callers now share a single in-flight promise resolving to the same object, so all their writes land.
- **`writeMemoryFile()`'s temp filename could collide** under rapid concurrent writes to the same path (pid + millisecond timestamp only) — added a random suffix.
- **Embedding cache write silently failed on a project's first-ever store** — `setCachedEmbedding()` is called before `appendEntry()` has created the scope root directory (embedding generation runs ahead of the actual file write in `store.ts`), so the cache write would throw `ENOENT` and get swallowed by the caller's catch-and-warn. Now ensures the directory exists before writing.

### Added

- **Secret-detection write gate** — `store_memory` now rejects content matching common credential formats (AWS keys, GitHub/Slack/OpenAI-style tokens, PEM private keys, generic `api_key: "..."` assignments) before it's ever written to disk, since stored memory is recalled into future conversations indefinitely.
- `retrieve_context`'s tool description now notes that returned content was written in a prior session and may contain untrusted text — a cheap prompt-injection-defense hardening for a coding agent that treats tool output as trusted.
- Test coverage for the secret-detection gate (unit + `store_memory` integration) and a dedicated `embedding-cache.test.ts` (including a concurrency regression test for the race fixed above).

### Changed

- **`memorykit init` now generates MCP configs using `npx -y memorykit-mcp-server@latest`** instead of a fixed `memorykit` binary command, for `.vscode/mcp.json`, `.mcp.json`, and `.cursor/mcp.json`. Every server launch now resolves the latest published version automatically — no more manually re-running `npm install -g` to stay current. A global install (`npm install -g memorykit-mcp-server`) is still supported for users who want a pinned version or to skip the per-launch `npx` resolution check; see the README's "Keeping MemoryKit up to date" section.

---

## [1.3.1] — 2026-07-22

### Fixed

- **`statusLine` written to wrong settings file** — `memorykit init` was writing `statusLine: { command: "..." }` to the project-scoped `.claude/settings.local.json`, which does not accept that key. This caused Claude Code to reject the entire file ("Settings file failed to parse — Invalid value"), disabling all hooks and permissions. The statusLine is now written to `~/.claude/settings.json` (user-level settings) where it is valid.

---

## [1.3.0] — 2026-07-22

### Added

- **`memorykit compress`** — new CLI command that compresses `.memorykit/` memory files using the local `claude --print` CLI (no API key required if Claude Code is installed). Strips filler from natural language fields while preserving all MML structure: headings, field keys, code blocks, inline code, tags, importance scores, and acquisition stats are never touched. Validates output before writing; retries once with a targeted fix prompt if validation fails. Backups stored in OS temp dir (outside `.memorykit/`) so they are never accidentally loaded as memory entries.
- **`memorykit statusline`** — new CLI command that outputs a one-line badge for the Claude Code status bar: `[MEMORYKIT] 🧠 47 entries · 8.2k saved`. `memorykit init` now writes `statusLine: { command: "memorykit statusline" }` to `.claude/settings.local.json` automatically. Outputs nothing if no memory exists (clean degradation).
- **Hook validation before settings writes** — `sanitizeHookSettings()` validates every hook entry's schema before writing `settings.local.json`. Prevents Claude Code from silently discarding the entire settings file due to a single malformed hook entry from another plugin.

### Fixed

- **JSONC parsing in settings merge** — `memorykit init` now strips `//` and `/* */` comments before parsing `settings.local.json`. Claude Code's own settings files sometimes contain comments; the raw `JSON.parse` would throw and silently skip hook installation for those users.

### Changed

- **Generated templates compressed** — `AGENTS.md`, `copilot-instructions.md`, `/recall` skill, `/save` skill, `.claude/rules/memory.md`, and the MCP server `instructions` field are all written in denser form. Same information, ~50% fewer words. Saves ~245 input tokens per session, permanently, for every project that runs `memorykit init`.

---

## [1.2.0] — 2026-07-21

### Added

- **`memorykit init` now generates Claude Code hooks** — writes `.claude/settings.local.json` with a `SessionStart` hook that calls `retrieve_context` automatically every time a session opens, before Claude reads the first message. Previously memory retrieval depended on Claude following CLAUDE.md instructions; now it is guaranteed by the framework. If the file already exists, the hook is merged rather than overwriting existing settings.
- **`/recall` skill** — `memorykit init` creates `.claude/skills/recall/SKILL.md`. A slash command Claude can invoke automatically (based on description match) or manually (`/recall auth module`). Instructs Claude to form narrow, specific queries — the key to retrieval precision.
- **`/save` skill** — `memorykit init` creates `.claude/skills/save/SKILL.md`. Guides Claude to write self-contained, WHY-focused memory content with the correct layer and `acquisition_context` for ROI tracking.
- **Path-scoped rules** — `memorykit init` creates `.claude/rules/memory.md` with a `paths` glob covering common source file patterns. The rule loads on-demand (zero startup cost) when Claude accesses matching files and nudges it to call `retrieve_context` with the specific module as the query.
- **MCP server `instructions` field** — the server now sends a concise instructions string in the MCP InitializeResult handshake. Claude reads this before any CLAUDE.md loads, ensuring the server is self-describing for users who never ran `memorykit init` (Claude Desktop, raw installs).
- **`alwaysLoad: true` in generated `.mcp.json`** — prevents Claude Code's tool search from deferring memorykit tool schemas. Tools are always visible from session open, no discovery latency.

### Changed

- `memorykit init` output now lists every generated file so developers can see exactly what was created.

---

## [1.1.1] — 2026-07-20

### Fixed

- **retrieve\_context token budget now actually applied** — retrieval was ignoring the Prefrontal Controller's per-query-type budgets and always using the flat `max_tokens_estimate` config value (default 4,000). It now uses the correct scoped budgets: ~200 for continuation, ~300 for procedural, ~500 for fact retrieval, ~1,500 for deep recall, ~2,000 for complex queries.
- **Quality gate duplicate thresholds now read from `memorykit.yaml`** — `checkDuplicate` was using hardcoded Jaccard (0.6) and word-overlap (3) values regardless of what was configured. `store_memory` now passes the loaded config thresholds.
- **Complex query routing missing episodes layer** — `resolveFiles` for the `Complex` query type omitted `episodes/*.md` from the project file list, so deep episode history was never searched on complex queries.
- **`forget_memory` now cleans the entity graph** — deleting an entry no longer leaves dangling references in the entity graph. Scope detection also fixed for Windows paths (uses `path.resolve` + `path.sep` to avoid prefix-collision between project names like `my-app` and `my-app-v2`).
- **Compaction used wrong field name** — `consolidate` wrote `content` instead of `what` when truncating long episode entries, producing entries the parser could not read back.
- README: tool count corrected to 7 (reflects `initialize_memory` added in 1.1.0), importance score range corrected to `0.1–0.95`, `max_tokens` default updated to reflect query-type budgets.

---

## [1.1.0] — 2026-07-14

### Added

- **`initialize_memory` MCP tool** — creates the memory directory structure from inside the MCP protocol (no CLI required). Idempotent; safe to call multiple times. Removes the hard dependency on running `memorykit init` before the server can accept tool calls.
- **`list_memories` tag filtering** — new `tags` parameter returns only entries matching any of the given tags across all layers.
- **`list_memories` content mode** — new `include_content:true` parameter returns entry title, content, and importance alongside counts. `max_entries` caps result size (default 50).
- **Cursor editor support in `memorykit init`** — generates `.cursor/mcp.json` alongside the existing `.mcp.json` and `.vscode/mcp.json` configs.
- **`AGENTS.md` as the canonical AI instruction file** — `memorykit init` now writes a single `AGENTS.md` with full MemoryKit instructions (including ROI tracking guidance and store-rejection handling). `CLAUDE.md` becomes a thin `@AGENTS.md` import so Claude Code, Copilot, and Cursor all read from one source.

### Fixed

- **Consolidation now triggers on `update_memory`** — previously auto-consolidation only fired after `store_memory`; updates were excluded, meaning long-running projects that primarily updated existing entries would never auto-consolidate.

---

## [1.0.1] — 2026-06-18

### Changed

- README: added a concise "token efficiency" and "accuracy" benefits section, grounded in the actual scoring/retrieval mechanisms (query-scoped token budgets, write-time quality gates, self-pruning lifecycle, measured ROI, hybrid semantic+keyword relevance). Previously this was only mentioned as a single parameter description. Docs-only release — no code changes.

---

## [1.0.0] — 2026-06-18

### Removed

- Dead code from the legacy Docker/.NET-API integration that the 0.2.0 changelog claimed was already removed but was actually still present: `src/api-client.ts`, `src/process-manager.ts`, `src/process-manager-dev.ts`, `src/index-dev.ts`, `src/tools/index.ts`, `test-docker.js`. None of these were ever included in the published npm package (excluded by the `files` whitelist), so this has no effect on already-published versions — pure source-tree hygiene.
- A stray empty `-p` directory at the package root, left over from a `mkdir -p` typo.

### Changed

- **BREAKING**: Package renamed from `memorykit` to `memorykit-mcp-server` for npm publishing — the unscoped `memorykit` name was already claimed by an unrelated, abandoned package. The CLI command remains `memorykit`; only the npm package name changed.
- `repository`, `bugs`, `homepage` fields corrected to match the actual GitHub remote (`rapozoantonio/memorykit`)
- `prepublishOnly` now runs `vitest run` (non-interactive) via a new `test:ci` script instead of `vitest` (watch mode), avoiding a hang on a local `npm publish`

### Fixed

- Two debug `console.log` calls in `retrieve.ts` could have corrupted the stdout JSON-RPC stream if `NODE_ENV=test` ever leaked into a real client launch — moved to `console.error`
- MCP server previously connected and accepted requests even with no memory directory initialized, failing opaquely on the first tool call — now exits with a clear instruction if neither project nor global memory is initialized
- `@xenova/transformers` was imported statically, so a native-binary load failure (e.g. on an unsupported CPU architecture) would have crashed the server at startup instead of degrading gracefully — import is now dynamic and lazy, loaded inside the existing try/catch
- Embedding model load had no timeout — a blocked or slow network could hang a tool call indefinitely; now times out after 30s and falls back to keyword-only search
- `memorykit --version` reported a hardcoded `"0.2.0"` string in `cli.ts`, independent of `package.json` — the 0.2.0 changelog claimed version drift was fixed, but that only covered the MCP handshake version in `server.ts`. Now reads from `package.json` the same way.

### Fixed (Linux-critical, caught by CI after first tag push)

- `sharp@0.32.6` (a hard transitive dependency of `@xenova/transformers`, not optional) crashed with `Module did not self-register` followed by a **segmentation fault** on `ubuntu-latest`, killing the whole process — this could not be caught by any try/catch since it's a native crash, not a JS exception. Forced via npm `overrides` to `sharp@^0.33.0` (resolved `0.33.5`), which moved to per-platform `@img/sharp-*` packages with far more reliable prebuilt binaries. Verified: full test suite green on both Windows and Ubuntu after the override.
- After the `sharp` fix, `onnxruntime-node`'s own internal binding probe (a separate native dependency, also transitive via `@xenova/transformers`) proved unstable on `ubuntu-latest` across three separate CI runs: an unhandled rejection, then (after upgrade attempts to `1.16.3`/`1.26.0` broke `@xenova/transformers@2.17.2`'s expected API in different ways and were reverted) a `free(): invalid pointer` heap-corruption abort on the exact-pinned `1.14.0`. Three different native-level failure modes from the same dependency on the same platform is a reliability problem in the dependency itself, not something fixable with a version pin or a try/catch. Added `process.on("unhandledRejection", ...)` handlers (in `server.ts` and a new `src/__tests__/setup.ts`) for the cases that are catchable, and set `MEMORYKIT_SKIP_EMBEDDINGS=true` for the Ubuntu leg of CI (only) so the test suite doesn't depend on this unstable native binding — Windows CI still exercises the real embedding path. Real users on Linux who hit this will fall back to keyword-only search per the existing graceful-degradation design; this is now a known, documented limitation rather than a silent risk.

### Added

- `SIGTERM`/`SIGINT` graceful shutdown handlers
- `MEMORYKIT_SKIP_EMBEDDINGS=true` environment variable to skip embedding generation entirely (airgapped/offline use)
- First-run log message when the embedding model is downloading
- CI: `mcp-server-test` job running the test suite on Ubuntu and Windows (previously never run in CI)
- CI: `mcp-server-publish` job, tag-gated on `v*`, publishes to npm

---

## [0.2.0] — 2026-03-04

### Fixed

- `quality_gates` config from `memorykit.yaml` was silently ignored — merged correctly now
- `layers` parameter in `retrieve_context` was accepted but never applied — now filters file patterns correctly
- Duplicate `ConsolidateResult` interface declaration in `types/memory.ts`
- Dead `formatTags()` function in `retrieve.ts` removed

### Changed

- `acquisition_context` parameter in `store_memory` is now **optional** (was incorrectly required)
- Server version now read dynamically from `package.json` — no more drift between files
- All tool handlers now validate input with Zod before processing
- File write operations are now serialized per-file path to prevent data loss under concurrent tool calls
- `axios` and `zod` removed as phantom dependencies (axios unused after legacy code removal)

### Removed

- Dead code from legacy Docker/.NET API architecture:
  - `src/api-client.ts` — HTTP client for removed .NET API
  - `src/process-manager.ts` — Docker lifecycle manager
  - `src/process-manager-dev.ts` — Dev-mode dotnet-run launcher
  - `src/index-dev.ts` — Legacy entry point
  - `src/tools/index.ts` — Old API-client-based tool registration
  - `test-docker.js` — Docker infrastructure test

### Added

- `.npmignore` — prevents tests, source, and dead code from being published
- `files` whitelist in `package.json` — only `dist/`, `templates/`, `README.md`, `LICENSE` are published
- `prepublishOnly` script — runs tests + build before every publish
- `repository`, `bugs`, `homepage` fields in `package.json`
- `exports` map for proper ESM subpath resolution
- `vitest.config.ts` — explicit test configuration
- Zod input validation schemas for all 6 MCP tools

---

## [0.1.0] — 2026-02-20

### Added

- Initial file-based MCP server with 6 tools: `store_memory`, `retrieve_context`, `update_memory`, `forget_memory`, `consolidate`, `list_memories`
- Brain-inspired 4-layer memory: Working, Facts, Episodes, Procedures
- Prefrontal Controller — query classification and intelligent file routing
- Amygdala Engine — 9-signal importance scoring (0.05–0.95)
- Write-time quality gates: importance floor, duplicate detection, contradiction warning
- Prose-to-MML normalization pipeline
- Auto consolidation: prune stale working memory, promote high-importance entries, compact old episodes
- CLI: `memorykit init`, `memorykit status`, `memorykit consolidate`
- Project isolation via git root detection (`~/.memorykit/<project-name>/`)
- Global memory scope (`~/.memorykit/`) shared across projects
- ROI tracking: acquisition context + retrieval savings display
