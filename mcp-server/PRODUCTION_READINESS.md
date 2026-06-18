# Production Readiness TRD — MemoryKit MCP Server

Status as of this document: code-level blockers are fixed and verified (build, 204 tests, `npm pack`, live JSON-RPC handshake all pass). What remains is account/infrastructure setup only you can do, one open hardware-verification gap, and a short list of non-blocking hardening work.

---

## 1. Blocking — requires your action, not code

These cannot be completed by an agent; they need your credentials or a judgment call.

### 1.1 ~~Create the `memorykit` npm organization~~ — not needed anymore
Originally planned to publish as `@memorykit/server`, but that scope is already claimed by someone else (confirmed when you tried creating it). Renamed to the unscoped `memorykit-mcp-server` instead — this is also the dominant real-world naming convention for community MCP servers (`firecrawl-mcp`, `exa-mcp-server`, `mem0-mcp`, etc.), doesn't depend on owning any npm org, and is already confirmed available. No org-creation step required.

### 1.2 ~~Add `NPM_TOKEN` secret to the GitHub repo~~ — done
Granular Access Token created (Packages and scopes: All packages, Read and write) and added as the `NPM_TOKEN` secret in the GitHub repo. The `mcp-server-publish` job will pick it up automatically on the next tag push.

Follow-up, not blocking: once `memorykit-mcp-server` exists in the registry after the first publish, consider generating a tighter token scoped to just that package and swapping the secret, then deleting the broad one.

### 1.3 ~~Decide the version/release strategy~~ — done
Bumped to `1.0.0` in `package.json`, with a matching CHANGELOG entry documenting the rename and fixes.

### 1.4 Trigger the first publish — the only remaining action
```bash
git add -A
git commit -m "Release 1.0.0: rename to memorykit-mcp-server, production-readiness fixes"
git tag v1.0.0
git push origin main
git push origin v1.0.0
```
This is the actual "go live" action — confirm you want it to run before pushing, since a published npm version cannot be unpublished after 72 hours (npm policy). Once the tag is pushed, watch the Actions tab: `mcp-server-test` runs first, then `mcp-server-publish` runs only if tests pass.

### 1.5 ARM64 Linux verification (open, can't be resolved by static analysis)
`@xenova/transformers`'s native dependency (`onnxruntime-node`) is confirmed to support `win32`/`darwin`/`linux` at the OS level (checked `package-lock.json`), but CPU architecture (x64 vs arm64) isn't declared there. The code now degrades gracefully if the embedding model fails to load (falls back to keyword-only search — verified in `store.ts`/`retrieve.ts`), so this is **not a crash risk**, but if you want to confidently claim ARM64 Linux/WSL support rather than "best-effort", it needs an actual run on that hardware (e.g. AWS Graviton, Raspberry Pi, or WSL2 on an ARM Windows host).

---

## 2. Recommended before launch — code-level, not yet done

Lower urgency than Section 1, but worth doing before or shortly after the first publish.

| Item | Why | File |
|---|---|---|
| Decouple debug logging from `NODE_ENV=test` | The stdout-pollution *symptom* is fixed (now `console.error`), but the debug logic is still gated on `NODE_ENV === "test"` — an env var that has nothing to do with "should I print debug info" and could be set for unrelated reasons in some hosting environments | `src/memory/retrieve.ts:263,291` |
| Add an `initialize_memory` MCP tool | Right now a user must run `memorykit init` in a terminal before the assistant can store/retrieve anything — breaks the "just talk to the AI" flow on first use | `src/tools/` (new file) |
| Add bounds validation to `memorykit.yaml` config | `importance_floor`, `duplicate_jaccard_threshold`, etc. are loaded with no min/max check — a typo'd config (e.g. `0.99`) silently breaks retrieval with no error | `src/storage/config-loader.ts` |
| Retry/backoff for background tasks | Entity indexing and auto-consolidation are fire-and-forget; failures are already caught and logged (verified — not silent), but there's no retry, so a transient failure is permanent until the next write | `src/memory/store.ts:178-206` |
| CHANGELOG entry | Document the rename (`memorykit` → `memorykit-mcp-server`) and fixes in this round before tagging, so consumers upgrading see why the package name changed | `CHANGELOG.md` |

---

## 3. Post-launch backlog — not blocking

- Structured logging (winston/pino) instead of raw `console.error` — fine for v1, matters more at scale.
- Memory/resource limits on embedding batch operations (no cap today on how many entries get embedded in one call).
- Backup/export tooling for the markdown-file memory store (no JSON/CSV export currently).
- Extend `mcp-server-test` CI matrix to `macos-latest` (currently Ubuntu + Windows only, per the original ask).

---

## What's already done and verified (recap)

- Renamed package to `memorykit-mcp-server` (unscoped — no npm org dependency); CLI command stays `memorykit` (independent fields).
- Fixed `repository`/`bugs`/`homepage` URLs to match actual git remote (`rapozoantonio/memorykit`).
- Fixed `prepublishOnly` to run tests non-interactively (`vitest run` via new `test:ci` script).
- Added CI job `mcp-server-test` (Ubuntu + Windows matrix) — `npm test` previously never ran in CI.
- Added CI job `mcp-server-publish`, tag-gated, `npm publish --access public`.
- Added startup guard in `server.ts` — clear error + exit if no memory directory is initialized, instead of an opaque failure on first tool call.
- Added `SIGTERM`/`SIGINT` graceful shutdown handlers.
- Fixed two `console.log` calls in `retrieve.ts` that could have corrupted the stdout JSON-RPC stream — now `console.error`.
- Hardened `embedding.ts`: dynamic import (so a native-binary load failure degrades gracefully instead of crashing the server at boot), 30s timeout on model load, `MEMORYKIT_SKIP_EMBEDDINGS=true` opt-out, first-run download log message.
- Bumped version to `1.0.0`, with a CHANGELOG entry documenting the rename and fixes.
- Fixed `memorykit --version` reporting a hardcoded `"0.2.0"` in `cli.ts`, independent of `package.json` (caught when verifying the version bump actually propagated) — now reads from `package.json` like `server.ts` already did.
- `NPM_TOKEN` GitHub secret added (Granular Access Token, All packages, Read and write).
- Verified: clean `tsc` build, 204/204 tests passing, `npm pack --dry-run` shows correct tarball contents (86.8kB, no source/test leakage) at `memorykit-mcp-server@1.0.0`, `--version` correctly reports `1.0.0`, and a live stdio `initialize` JSON-RPC round-trip confirmed stdout carries only protocol messages.

**The only remaining blocking step is 1.4 above: commit, tag `v1.0.0`, and push.**
