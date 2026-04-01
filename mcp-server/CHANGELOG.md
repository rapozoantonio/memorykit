# Changelog

All notable changes to MemoryKit MCP Server are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
