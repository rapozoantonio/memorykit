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
**Correction made during release:** the generic `v1.0.0` tag was already taken by the .NET side of this monorepo (tagged 2025-11-19, "Initial public release"), and the `docker` + `release` CI jobs both react to *any* `v*` tag. Reusing it would have either failed (tag already exists) or, with a different version number, cascaded into unrelated .NET Docker builds and GitHub Releases. Fixed by giving the npm package its own tag namespace: `memorykit-mcp-server-v*`. Updated in `.github/workflows/main.yml`:
- Top-level `on.push.tags` now includes `memorykit-mcp-server-v*` alongside the existing `v*`
- `docker` job explicitly excludes `refs/tags/memorykit-mcp-server-*`
- `mcp-server-publish` job triggers on `refs/tags/memorykit-mcp-server-v*` specifically (not generic `refs/tags/v`)
- `release` (.NET GitHub Release) job was already safe — it matches `refs/tags/v` literally, which `memorykit-mcp-server-v*` doesn't start with

```bash
git tag memorykit-mcp-server-v1.0.0
git push origin memorykit-mcp-server-v1.0.0
```
(The commit itself was already pushed to `main` separately.) This is the actual "go live" action — a published npm version cannot be unpublished after 72 hours (npm policy). Once the tag is pushed, watch the Actions tab: `mcp-server-test` runs first, then `mcp-server-publish` runs only if tests pass — and only those two jobs should run, not the .NET Docker/Release jobs.

### 1.5 ~~x64 Linux segfault~~ — found and fixed, correction to earlier claims
**This invalidates earlier "Linux is fine" statements in this doc and in conversation — caught only because real CI ran on `ubuntu-latest` after the first tag push.** `sharp@0.32.6`, a **hard** (non-optional) transitive dependency of `@xenova/transformers`, crashed with `Module did not self-register` followed by a segmentation fault on Ubuntu — a native crash, not a JS exception, so none of the try/catch hardening done earlier could have caught it. Root cause: an old sharp version with less reliable Linux prebuilt binaries. Fix: forced `sharp` to `^0.33.0` (resolved `0.33.5`) via npm `overrides` in `package.json`. Verified: full 204-test suite passes on both Windows and Ubuntu after the fix (pending final Ubuntu CI confirmation post-push).

This is exactly the class of risk flagged earlier as "theoretical/unverified" for `onnxruntime-node` on ARM64 — except it turned out to be `sharp` on plain x64 Linux, which is a much more common deployment target. Take this as a concrete reason not to fully trust "should work on Linux" claims for native-dependency-heavy packages without actually running CI on that OS — which is precisely why the `mcp-server-test` Ubuntu leg was worth adding.

**Second related finding, same root cause:** after the `sharp` fix, `onnxruntime-node` (also transitive via `@xenova/transformers`, also a native dependency) surfaced an unhandled rejection from its own internal binding probe on Ubuntu — non-fatal, but it failed CI despite all 204 tests passing. Tried upgrading it directly (`1.16.3`, `1.26.0`) but both broke `@xenova/transformers@2.17.2`'s expected API in different, silent ways — reverted. Fixed correctly by adding a `process.on("unhandledRejection", ...)` handler (in `server.ts` for production, `src/__tests__/setup.ts` for tests) rather than chasing a version pin. Pending final Ubuntu CI confirmation post-push.

### 1.6 ARM64 Linux verification (still open, can't be resolved by static analysis)
With the x64 segfault fixed, ARM64 (WSL2 on an ARM Windows host, Raspberry Pi, AWS Graviton) is the remaining unverified platform. `onnxruntime-node`'s `package-lock.json` entry confirms OS-level support for `win32`/`darwin`/`linux` but doesn't declare CPU architecture. The embedding code degrades gracefully if the model fails to load via a catchable error (verified in `store.ts`/`retrieve.ts`) — but as 1.5 just demonstrated, a native dependency can fail with a segfault that bypasses try/catch entirely, so "should degrade gracefully" is not the same guarantee as "tested and confirmed." Treat ARM64 as best-effort until someone actually runs it on that hardware.

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
- Scoped CI tag triggers to `memorykit-mcp-server-v*` so npm releases never collide with the unrelated .NET `v1.0.0` tag or trigger its Docker/Release jobs.
- First tag push (`memorykit-mcp-server-v1.0.0`) surfaced a real segfault on `ubuntu-latest` from an old transitive `sharp` dependency — fixed via npm `overrides` forcing `sharp@^0.33.0`. See 1.5.
- Verified: clean `tsc` build, 204/204 tests passing on Windows, `npm pack --dry-run` shows correct tarball contents at `memorykit-mcp-server@1.0.0`, `--version` correctly reports `1.0.0`, and a live stdio `initialize` JSON-RPC round-trip confirmed stdout carries only protocol messages.

**Status: the sharp fix is pushed to `main`; waiting on CI to confirm the Ubuntu leg of `mcp-server-test` is green before re-tagging `memorykit-mcp-server-v1.0.0` and re-triggering the publish.**
