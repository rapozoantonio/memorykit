# Changelog

All notable changes to MemoryKit MCP Server are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] — 2026-06-18

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
