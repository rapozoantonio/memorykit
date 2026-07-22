/**
 * CLI commands - init, status, consolidate, compress, statusline
 */

import { Command } from "commander";
import {
  mkdirSync,
  writeFileSync,
  existsSync,
  readFileSync,
  readdirSync,
  statSync,
} from "fs";
import { join, dirname, basename } from "path";
import { homedir, tmpdir } from "os";
import { fileURLToPath } from "url";
import { spawnSync } from "child_process";

const _pkgPath = join(
  dirname(fileURLToPath(import.meta.url)),
  "..",
  "package.json",
);
const _pkg = JSON.parse(readFileSync(_pkgPath, "utf-8")) as { version: string };
import { MemoryLayer, MemoryScope } from "./types/memory.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
  getWorkingDirectory,
  isProjectInitialized,
  isGlobalInitialized,
} from "./storage/scope-resolver.js";
import { listMemoryFiles, getTotalEntryCount } from "./storage/file-manager.js";
import { consolidateMemory } from "./memory/consolidate.js";
import { loadConfig, getDefaultConfig } from "./storage/config-loader.js";
import { stringify as stringifyYaml } from "yaml";

// ── JSONC parser ──────────────────────────────────────────────────────────────
// Claude Code's settings files sometimes contain // comments that break JSON.parse.
// Strip them before parsing so the merge never silently fails.
function stripJsonComments(src: string): string {
  let out = "";
  let i = 0;
  const n = src.length;
  let inString = false;
  let stringChar = "";
  let inLine = false;
  let inBlock = false;
  while (i < n) {
    const c = src[i];
    const next = i + 1 < n ? src[i + 1] : "";
    if (inLine) {
      if (c === "\n") { inLine = false; out += c; }
      i++; continue;
    }
    if (inBlock) {
      if (c === "*" && next === "/") { inBlock = false; i += 2; } else i++;
      continue;
    }
    if (inString) {
      out += c;
      if (c === "\\" && i + 1 < n) { out += src[i + 1]; i += 2; continue; }
      if (c === stringChar) inString = false;
      i++; continue;
    }
    if (c === '"' || c === "'") { inString = true; stringChar = c; out += c; i++; continue; }
    if (c === "/" && next === "/") { inLine = true; i += 2; continue; }
    if (c === "/" && next === "*") { inBlock = true; i += 2; continue; }
    out += c; i++;
  }
  return out;
}

// ── Hook validation ───────────────────────────────────────────────────────────
// Claude Code silently discards the entire settings.json if Zod schema fails.
// Validate hook entries before every write to prevent silent hook failures.
function isValidHookEntry(hook: unknown): boolean {
  if (!hook || typeof hook !== "object") return false;
  const h = hook as Record<string, unknown>;
  if (!h.type || typeof h.type !== "string") return false;
  if (h.type === "mcp_tool") return typeof h.server === "string" && typeof h.tool === "string";
  if (h.type === "command" || h.type === "bash") return typeof h.command === "string";
  return true;
}

function sanitizeHookSettings(settings: Record<string, unknown>): Record<string, unknown> {
  const hooks = settings.hooks as Record<string, unknown[]> | undefined;
  if (!hooks || typeof hooks !== "object") return settings;
  for (const eventName of Object.keys(hooks)) {
    const entries = hooks[eventName];
    if (!Array.isArray(entries)) { delete hooks[eventName]; continue; }
    hooks[eventName] = entries.filter((entry: unknown) => {
      if (!entry || typeof entry !== "object") return false;
      const e = entry as Record<string, unknown>;
      const rawHooks = e.hooks;
      if (!Array.isArray(rawHooks)) return false;
      const filtered = (rawHooks as unknown[]).filter(isValidHookEntry);
      e.hooks = filtered;
      return filtered.length > 0;
    });
  }
  return settings;
}

/**
 * Initialize .memorykit/ directory structure
 */
export async function initCommand(options: {
  global?: boolean;
}): Promise<void> {
  const root = options.global ? resolveGlobalRoot() : resolveProjectRoot();

  if (existsSync(root)) {
    console.log(`✅ Memory already initialized at: ${root}`);
  } else {
    mkdirSync(root, { recursive: true });
    mkdirSync(join(root, MemoryLayer.Working), { recursive: true });
    mkdirSync(join(root, MemoryLayer.Facts), { recursive: true });
    mkdirSync(join(root, MemoryLayer.Episodes), { recursive: true });
    mkdirSync(join(root, MemoryLayer.Procedures), { recursive: true });

    const configPath = join(root, "memorykit.yaml");
    writeFileSync(configPath, stringifyYaml(getDefaultConfig()), "utf-8");

    const sessionPath = join(root, MemoryLayer.Working, "session.md");
    writeFileSync(sessionPath, `# Working Memory\n\nCurrent session context and active tasks.\n\n---\n`, "utf-8");

    writeFileSync(join(root, MemoryLayer.Facts, ".gitkeep"), "", "utf-8");
    writeFileSync(join(root, MemoryLayer.Episodes, ".gitkeep"), "", "utf-8");
    writeFileSync(join(root, MemoryLayer.Procedures, ".gitkeep"), "", "utf-8");

    console.log(`✅ Initialized MemoryKit at: ${root}`);
    console.log(`\nStructure:`);
    console.log(`  ${root}/`);
    console.log(`  ├── memorykit.yaml`);
    console.log(`  ├── working/session.md`);
    console.log(`  ├── facts/`);
    console.log(`  ├── episodes/`);
    console.log(`  └── procedures/`);
  }

  if (!options.global) {
    const workingDir = getWorkingDirectory();

    // VS Code Copilot config
    const vscodeDir = join(workingDir, ".vscode");
    const mcpConfigPath = join(vscodeDir, "mcp.json");
    if (!existsSync(mcpConfigPath)) {
      mkdirSync(vscodeDir, { recursive: true });
      writeFileSync(mcpConfigPath, JSON.stringify({
        servers: {
          memorykit: {
            type: "stdio",
            command: "memorykit",
            env: { MEMORYKIT_PROJECT: "${workspaceFolder}" },
          },
        },
      }, null, 2), "utf-8");
      console.log(`\n✅ Created VS Code Copilot config: ${mcpConfigPath}`);
      console.log(`💡 Reload VS Code for changes to take effect`);
    } else {
      console.log(`\n⚠️  .vscode/mcp.json already exists, skipping`);
    }

    // Claude Code MCP config
    const claudeMcpConfigPath = join(workingDir, ".mcp.json");
    if (!existsSync(claudeMcpConfigPath)) {
      writeFileSync(claudeMcpConfigPath, JSON.stringify({
        mcpServers: {
          memorykit: {
            command: "memorykit",
            alwaysLoad: true,
            args: [],
            env: {},
          },
        },
      }, null, 2), "utf-8");
      console.log(`\n✅ Created Claude Code config: ${claudeMcpConfigPath}`);
      console.log(`💡 Claude Code will auto-detect this server in the project`);
    } else {
      console.log(`\n⚠️  .mcp.json already exists, skipping`);
    }

    // Cursor config
    const cursorDir = join(workingDir, ".cursor");
    const cursorMcpConfigPath = join(cursorDir, "mcp.json");
    if (!existsSync(cursorMcpConfigPath)) {
      mkdirSync(cursorDir, { recursive: true });
      writeFileSync(cursorMcpConfigPath, JSON.stringify({
        mcpServers: {
          memorykit: {
            command: "memorykit",
            env: { MEMORYKIT_PROJECT: "${workspaceFolder}" },
          },
        },
      }, null, 2), "utf-8");
      console.log(`\n✅ Created Cursor config: ${cursorMcpConfigPath}`);
      console.log(`💡 Restart Cursor for changes to take effect`);
    } else {
      console.log(`\n⚠️  .cursor/mcp.json already exists, skipping`);
    }

    // AGENTS.md — compressed: ~100 words vs original 230 (~170 tokens saved per session)
    const agentsMdPath = join(workingDir, "AGENTS.md");
    const agentsMdContent = `## Memory System (MemoryKit)

Before any task: call \`retrieve_context\` with a narrow, specific query (module name, bug, topic) — not a generic one.

When completing work, call \`store_memory\` with the right layer:
- **facts**: architecture decisions, tech choices, constraints
- **episodes**: bugs fixed, failed approaches, root causes
- **procedures**: patterns, conventions, workflows

Always include WHY, not just WHAT.

ROI: If discovery took real effort, pass \`acquisition_context\`:
\`store_memory({ content: "...", acquisition_context: { tokens_consumed: 800, tool_calls: 3 } })\`

Rejection handling — check \`suggestion\` field:
- \`near-duplicate\`: call \`update_memory\` with returned \`entry_id\`
- \`importance-floor\`: too routine, skip
- \`stored: true\` + \`warning\`: contradiction — update stale entry
`;

    if (!existsSync(agentsMdPath)) {
      writeFileSync(agentsMdPath, agentsMdContent, "utf-8");
      console.log(`\n✅ Created agent instructions: ${agentsMdPath}`);
      console.log(`💡 Read by Claude Code, GitHub Copilot, and Cursor`);
    } else {
      const existingAgentsContent = readFileSync(agentsMdPath, "utf-8");
      if (!existingAgentsContent.includes("## Memory System (MemoryKit)")) {
        writeFileSync(agentsMdPath, existingAgentsContent + "\n" + agentsMdContent, "utf-8");
        console.log(`\n✅ Added MemoryKit section to: ${agentsMdPath}`);
      } else {
        console.log(`\n⚠️  AGENTS.md already has MemoryKit instructions, skipping`);
      }
    }

    // CLAUDE.md — thin import of AGENTS.md
    const claudeMdPath = join(workingDir, "CLAUDE.md");
    const claudeMdContent = `@AGENTS.md\n`;
    if (!existsSync(claudeMdPath)) {
      writeFileSync(claudeMdPath, claudeMdContent, "utf-8");
      console.log(`\n✅ Created Claude Code instructions: ${claudeMdPath}`);
    } else {
      const existingContent = readFileSync(claudeMdPath, "utf-8");
      if (!existingContent.includes("@AGENTS.md")) {
        writeFileSync(claudeMdPath, existingContent + "\n\n" + claudeMdContent, "utf-8");
        console.log(`\n✅ Added MemoryKit section to: ${claudeMdPath}`);
      } else {
        console.log(`\n⚠️  CLAUDE.md already has MemoryKit instructions, skipping`);
      }
    }

    // .github/copilot-instructions.md — compressed
    const githubDir = join(workingDir, ".github");
    const copilotInstructionsPath = join(githubDir, "copilot-instructions.md");
    const memoryKitInstructions = `
## Memory System (MemoryKit)

Before any task: call \`retrieve_context\` with a narrow, specific query.

When completing work:
- \`store_memory\` with layer — **facts**: decisions/tech choices, **episodes**: bugs/root causes, **procedures**: patterns/workflows
- Always include WHY, not just WHAT

ROI: \`store_memory({ content: "...", acquisition_context: { tokens_consumed: 800, tool_calls: 3 } })\`

Rejection handling (\`suggestion\` field):
- \`near-duplicate\`: call \`update_memory\` with \`entry_id\`
- \`importance-floor\`: too routine, skip
- \`stored: true\` + \`warning\`: contradiction — update stale entry
`;

    if (!existsSync(copilotInstructionsPath)) {
      mkdirSync(githubDir, { recursive: true });
      writeFileSync(copilotInstructionsPath, memoryKitInstructions.trim() + "\n", "utf-8");
      console.log(`\n✅ Created Copilot instructions: ${copilotInstructionsPath}`);
    } else {
      const existingContent = readFileSync(copilotInstructionsPath, "utf-8");
      if (!existingContent.includes("## Memory System (MemoryKit)")) {
        writeFileSync(copilotInstructionsPath, existingContent + "\n" + memoryKitInstructions, "utf-8");
        console.log(`\n✅ Added MemoryKit section to: ${copilotInstructionsPath}`);
      } else {
        console.log(`\n⚠️  MemoryKit instructions already in copilot-instructions.md, skipping`);
      }
    }

    // .claude/settings.local.json — SessionStart hook + statusline
    const claudeSettingsDir = join(workingDir, ".claude");
    const settingsLocalPath = join(claudeSettingsDir, "settings.local.json");
    mkdirSync(claudeSettingsDir, { recursive: true });

    const memorykitHook = {
      matcher: ".*",
      hooks: [
        {
          type: "mcp_tool",
          server: "memorykit",
          tool: "retrieve_context",
          input: {
            query: "what was I working on and what decisions were made",
            scope: "all",
          },
        },
      ],
    };

    if (!existsSync(settingsLocalPath)) {
      const newSettings = sanitizeHookSettings({
        hooks: { SessionStart: [memorykitHook] },
      });
      writeFileSync(settingsLocalPath, JSON.stringify(newSettings, null, 2), "utf-8");
      console.log(`\n✅ Created Claude Code hooks: ${settingsLocalPath}`);
    } else {
      try {
        const existing = JSON.parse(stripJsonComments(readFileSync(settingsLocalPath, "utf-8")));
        let changed = false;

        const hasHook = existing.hooks?.SessionStart?.some((h: unknown) => {
          const he = h as Record<string, unknown>;
          return Array.isArray(he.hooks) &&
            (he.hooks as unknown[]).some((hh: unknown) => {
              const hook = hh as Record<string, unknown>;
              return hook.server === "memorykit" && hook.tool === "retrieve_context";
            });
        });

        if (!hasHook) {
          existing.hooks = existing.hooks ?? {};
          existing.hooks.SessionStart = existing.hooks.SessionStart ?? [];
          existing.hooks.SessionStart.push(memorykitHook);
          changed = true;
          console.log(`\n✅ Added MemoryKit hook to: ${settingsLocalPath}`);
        } else {
          console.log(`\n⚠️  MemoryKit hook already in settings.local.json, skipping`);
        }

        if (changed) {
          writeFileSync(
            settingsLocalPath,
            JSON.stringify(sanitizeHookSettings(existing), null, 2),
            "utf-8",
          );
        }
      } catch {
        console.log(`\n⚠️  .claude/settings.local.json exists but couldn't be parsed, skipping`);
      }
    }

    // /recall skill — compressed
    const recallSkillDir = join(claudeSettingsDir, "skills", "recall");
    const recallSkillPath = join(recallSkillDir, "SKILL.md");
    if (!existsSync(recallSkillPath)) {
      mkdirSync(recallSkillDir, { recursive: true });
      writeFileSync(recallSkillPath, `---
name: recall
description: Retrieve relevant memories for the current task. Use when starting a task, switching context, or asking about past decisions.
allowed-tools: mcp__memorykit__retrieve_context
---

Call retrieve_context with the task or topic as query — be precise.
Use /recall argument directly if provided (e.g. /recall auth module → query: "auth module").
Otherwise derive query from what the user is doing. Return results without reformatting.
`, "utf-8");
      console.log(`\n✅ Created /recall skill: ${recallSkillPath}`);
    } else {
      console.log(`\n⚠️  /recall skill already exists, skipping`);
    }

    // /save skill — compressed
    const saveSkillDir = join(claudeSettingsDir, "skills", "save");
    const saveSkillPath = join(saveSkillDir, "SKILL.md");
    if (!existsSync(saveSkillPath)) {
      mkdirSync(saveSkillDir, { recursive: true });
      writeFileSync(saveSkillPath, `---
name: save
description: Save a discovery, decision, or pattern to memorykit. Use after fixing a bug, making an architecture decision, establishing a convention, or completing significant work.
allowed-tools: mcp__memorykit__store_memory
---

Call store_memory with precise, self-contained content. Include WHY, not just WHAT.

Layer:
- facts: architecture decisions, tech choices, permanent constraints
- episodes: bugs fixed, failed approaches, root causes
- procedures: coding patterns, conventions, repeatable workflows
- working: active task context (auto-expires 7 days)

If discovery took real investigation, pass acquisition_context with tokens_consumed and tool_calls.
`, "utf-8");
      console.log(`\n✅ Created /save skill: ${saveSkillPath}`);
    } else {
      console.log(`\n⚠️  /save skill already exists, skipping`);
    }

    // ~/.claude/settings.json — user-level statusLine (not project settings.local.json)
    // statusLine is valid only in user settings, not project-scoped settings.local.json
    const userClaudeDir = join(homedir(), ".claude");
    const userSettingsPath = join(userClaudeDir, "settings.json");
    try {
      let userSettings: Record<string, unknown> = {};
      if (existsSync(userSettingsPath)) {
        userSettings = JSON.parse(stripJsonComments(readFileSync(userSettingsPath, "utf-8")));
      }
      if (!userSettings.statusLine) {
        userSettings.statusLine = { command: "memorykit statusline" };
        mkdirSync(userClaudeDir, { recursive: true });
        writeFileSync(userSettingsPath, JSON.stringify(userSettings, null, 2), "utf-8");
        console.log(`\n✅ Added statusLine to: ${userSettingsPath}`);
      } else {
        console.log(`\n⚠️  statusLine already set in ~/.claude/settings.json, skipping`);
      }
    } catch {
      console.log(`\n⚠️  Could not update ~/.claude/settings.json for statusLine, skipping`);
    }

    // .claude/rules/memory.md — compressed
    const rulesDir = join(claudeSettingsDir, "rules");
    const rulesPath = join(rulesDir, "memory.md");
    if (!existsSync(rulesPath)) {
      mkdirSync(rulesDir, { recursive: true });
      writeFileSync(rulesPath, `---
paths:
  - "src/**/*"
  - "*.ts"
  - "*.py"
  - "*.go"
  - "*.rs"
  - "*.java"
  - "*.kt"
---

Before modifying this file: if retrieve_context hasn't been called for this module, call it with the filename or module name as query. After finding a bug root cause or constraint: call store_memory.
`, "utf-8");
      console.log(`\n✅ Created path rules: ${rulesPath}`);
    } else {
      console.log(`\n⚠️  .claude/rules/memory.md already exists, skipping`);
    }
  }
}

/**
 * Show memory statistics
 */
export async function statusCommand(): Promise<void> {
  const projectInitialized = isProjectInitialized();
  const globalInitialized = isGlobalInitialized();

  if (!projectInitialized && !globalInitialized) {
    console.log("❌ No memory initialized. Run `memorykit init` to get started.");
    return;
  }

  console.log("📊 MemoryKit Status\n");

  if (projectInitialized) {
    const projectRoot = resolveProjectRoot();
    const projectFiles = await listMemoryFiles(projectRoot);
    const projectEntries = await getTotalEntryCount(projectRoot);

    console.log(`📁 Project Memory: ${projectRoot}`);
    console.log(`   Total entries: ${projectEntries}`);

    const byLayer = groupFilesByLayer(projectFiles);
    for (const [layer, files] of Object.entries(byLayer)) {
      console.log(
        `   ${layer}: ${files.length} files, ${files.reduce((sum: number, f: any) => sum + f.entryCount, 0)} entries`,
      );
    }
    console.log("");
  }

  if (globalInitialized) {
    const globalRoot = resolveGlobalRoot();
    const globalFiles = await listMemoryFiles(globalRoot);
    const globalEntries = await getTotalEntryCount(globalRoot);

    console.log(`🌍 Global Memory: ${globalRoot}`);
    console.log(`   Total entries: ${globalEntries}`);

    const byLayer = groupFilesByLayer(globalFiles);
    for (const [layer, files] of Object.entries(byLayer)) {
      console.log(
        `   ${layer}: ${files.length} files, ${files.reduce((sum: number, f: any) => sum + f.entryCount, 0)} entries`,
      );
    }
  }
}

/**
 * Manual consolidation command
 */
export async function consolidateCommand(options: {
  scope?: string;
  dryRun?: boolean;
}): Promise<void> {
  const scope = (options.scope || "project") as "project" | "global" | "all";
  const dryRun = options.dryRun ?? false;

  console.log(`🧹 Consolidating ${scope} memory${dryRun ? " (dry run)" : ""}...\n`);

  const result = await consolidateMemory({ scope, dry_run: dryRun });

  console.log(`✅ Consolidation complete:`);
  console.log(`   Pruned: ${result.pruned}`);
  console.log(`   Promoted: ${result.promoted}`);
  console.log(`   Compacted: ${result.compacted}`);

  if (result.details.length > 0) {
    console.log(`\nDetails:`);
    for (const action of result.details.slice(0, 10)) {
      console.log(`   - ${action.action}: ${action.entry_id} ${action.reason || ""}`);
    }
    if (result.details.length > 10) {
      console.log(`   ... and ${result.details.length - 10} more actions`);
    }
  }
}

/**
 * Statusline command — one-line badge for Claude Code status bar.
 * Wired via settings.local.json { statusLine: { command: "memorykit statusline" } }
 * Output: [MEMORYKIT] 🧠 47 entries · 8.2k saved
 * Outputs nothing if no memory exists (statusline stays clean).
 */
export async function statuslineCommand(): Promise<void> {
  const roots: string[] = [];
  if (isProjectInitialized()) roots.push(resolveProjectRoot());
  if (isGlobalInitialized()) roots.push(resolveGlobalRoot());
  if (roots.length === 0) return;

  let totalEntries = 0;
  let totalAcquisitionTokens = 0;

  for (const root of roots) {
    const stats = readStatuslineStats(root);
    totalEntries += stats.entries;
    totalAcquisitionTokens += stats.acquisitionTokens;
  }

  if (totalEntries === 0) return;

  const savedStr =
    totalAcquisitionTokens >= 1000
      ? `${(totalAcquisitionTokens / 1000).toFixed(1)}k`
      : `${totalAcquisitionTokens}`;

  process.stdout.write(`[MEMORYKIT] 🧠 ${totalEntries} entries · ${savedStr} saved\n`);
}

/**
 * Quick scan of .memorykit/ files for statusline stats.
 * Counts ### headings (entries) and sums acquisition tokens without full parsing.
 */
function readStatuslineStats(root: string): { entries: number; acquisitionTokens: number } {
  let entries = 0;
  let acquisitionTokens = 0;
  const acqRe = /^- \*\*acquisition\*\*:\s*(\d+)t,/gm;

  function scanDir(dir: string): void {
    if (!existsSync(dir)) return;
    for (const name of readdirSync(dir)) {
      const fp = join(dir, name);
      try {
        if (statSync(fp).isDirectory()) { scanDir(fp); continue; }
        if (!name.endsWith(".md") || name === ".gitkeep") continue;
        const content = readFileSync(fp, "utf-8");
        entries += (content.match(/^### /mg) ?? []).length;
        for (const m of content.matchAll(acqRe)) {
          acquisitionTokens += parseInt(m[1], 10);
        }
      } catch {
        // Skip unreadable files — statusline must never crash
      }
    }
  }

  scanDir(root);
  return { entries, acquisitionTokens };
}

/**
 * Compress memory files — reduces input token cost of stored memories.
 * Uses the local `claude --print` CLI (no API key needed if Claude Code is installed).
 * Backs up originals to OS temp dir before overwriting.
 */
export async function compressCommand(options: {
  scope?: string;
  dryRun?: boolean;
}): Promise<void> {
  const scope = (options.scope || "project") as "project" | "global" | "all";
  const dryRun = options.dryRun ?? false;

  const roots: string[] = [];
  if ((scope === "project" || scope === "all") && isProjectInitialized()) roots.push(resolveProjectRoot());
  if ((scope === "global" || scope === "all") && isGlobalInitialized()) roots.push(resolveGlobalRoot());

  if (roots.length === 0) {
    console.log("❌ No memory initialized for the selected scope. Run `memorykit init` first.");
    return;
  }

  // Verify claude CLI is available before processing any files
  const claudeCheck = spawnSync("claude", ["--version"], { encoding: "utf-8" });
  if (claudeCheck.error) {
    console.log("❌ claude CLI not found. Install Claude Code from https://claude.ai/code to use memorykit compress.");
    return;
  }

  console.log(`🗜️  Compressing ${scope} memory${dryRun ? " (dry run)" : ""}...\n`);

  let compressed = 0;
  let skipped = 0;
  let failed = 0;

  for (const root of roots) {
    for (const filePath of collectMdFiles(root)) {
      const result = compressMemoryFile(filePath, dryRun);
      if (result === "compressed") compressed++;
      else if (result === "skipped") skipped++;
      else failed++;
    }
  }

  console.log(`\n✅ Done: ${compressed} compressed, ${skipped} skipped, ${failed} failed`);
  if (dryRun) console.log("   Dry run — no files modified");
  if (!dryRun && compressed > 0) {
    const backupDir = join(tmpdir(), "memorykit-compress-backups");
    console.log(`📦 Backups saved to: ${backupDir}`);
  }
}

function collectMdFiles(dir: string): string[] {
  const files: string[] = [];
  if (!existsSync(dir)) return files;
  for (const name of readdirSync(dir)) {
    const fp = join(dir, name);
    try {
      if (statSync(fp).isDirectory()) { files.push(...collectMdFiles(fp)); continue; }
      // Skip .gitkeep, backups, and config files
      if (!name.endsWith(".md") || name === ".gitkeep" || name.endsWith(".original.md")) continue;
      files.push(fp);
    } catch { /* skip */ }
  }
  return files;
}

function compressMemoryFile(filePath: string, dryRun: boolean): "compressed" | "skipped" | "failed" {
  let original: string;
  try {
    original = readFileSync(filePath, "utf-8");
  } catch {
    console.log(`  ❌ Cannot read: ${basename(filePath)}`);
    return "failed";
  }

  if (!original.trim() || original.length < 200) {
    console.log(`  ⏭  Too short to compress: ${basename(filePath)}`);
    return "skipped";
  }

  // Count entries — skip files with no MML entries (e.g. session.md with just notes)
  const entryCount = (original.match(/^### /mg) ?? []).length;
  if (entryCount === 0) {
    console.log(`  ⏭  No entries: ${basename(filePath)}`);
    return "skipped";
  }

  const kb = (original.length / 1024).toFixed(1);
  console.log(`  🗜️  ${basename(filePath)} (${kb}KB, ${entryCount} entries)${dryRun ? " [dry run]" : ""}`);

  if (dryRun) return "compressed";

  try {
    const compressed = callClaudeForCompression(buildCompressPrompt(original));

    if (!compressed || compressed.trim() === original.trim()) {
      console.log(`     ⏭  Already compact — no change`);
      return "skipped";
    }

    const errors = validateMmlCompression(original, compressed);
    if (errors.length > 0) {
      console.log(`     ⚠️  Validation failed: ${errors.join(", ")} — retrying`);
      const fixed = callClaudeForCompression(buildFixPrompt(original, compressed, errors));
      const fixErrors = validateMmlCompression(original, fixed);
      if (fixErrors.length > 0) {
        console.log(`     ❌ Still invalid after retry — skipping to preserve data`);
        return "failed";
      }
      writeCompressedFile(filePath, original, fixed);
    } else {
      writeCompressedFile(filePath, original, compressed);
    }

    const savings = Math.round((1 - compressed.length / original.length) * 100);
    console.log(`     ✅ ${savings}% smaller`);
    return "compressed";
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    console.log(`     ❌ Failed: ${msg}`);
    return "failed";
  }
}

function buildCompressPrompt(content: string): string {
  return `Compress this MemoryKit memory file. Remove filler, articles, hedging. Use fragments. Keep all technical substance.

NEVER modify:
- ### headings (entry titles) — copy exactly
- - **key**: lines — preserve "- **key**:" format; compress only the VALUE text
- Code blocks (\`\`\` ... \`\`\`) — copy byte-for-byte
- Inline code (\`...\`) — copy byte-for-byte
- Field values for tags, importance, created, acquisition — copy exactly
- File paths, function names, error messages, library/API names

COMPRESS natural language in: what, why, constraint, symptom, fix, root-cause, workaround field values.
Drop: a/an/the, filler (basically/essentially/simply), hedging (might/should/consider).
Fragments OK: "New ref each render" not "A new reference is created on each render cycle".

Return ONLY the compressed file. No explanation. No outer code fence.

FILE:
${content}`;
}

function buildFixPrompt(original: string, compressed: string, errors: string[]): string {
  return `Fix these validation errors in the compressed file. ONLY fix listed errors — do not recompress or change anything else.

ERRORS:
${errors.map((e) => `- ${e}`).join("\n")}

ORIGINAL (reference only — to restore missing content):
${original}

COMPRESSED (fix this):
${compressed}

Return ONLY the fixed file. No explanation.`;
}

function callClaudeForCompression(prompt: string): string {
  const result = spawnSync("claude", ["--print"], {
    input: prompt,
    encoding: "utf-8",
    timeout: 120_000,
    maxBuffer: 10 * 1024 * 1024,
  });
  if (result.error) {
    if ((result.error as NodeJS.ErrnoException).code === "ENOENT") {
      throw new Error("claude CLI not found");
    }
    throw new Error(result.error.message);
  }
  if (result.status !== 0) {
    throw new Error(`claude exited ${result.status}: ${result.stderr?.slice(0, 200)}`);
  }
  // Strip outer ```markdown fence if Claude wraps its output
  return result.stdout.trim().replace(/^```(?:markdown)?\n([\s\S]*)\n```$/s, "$1");
}

function validateMmlCompression(original: string, compressed: string): string[] {
  const errors: string[] = [];

  const origHeadings = (original.match(/^#{1,6}\s+.+$/mg) ?? []).length;
  const compHeadings = (compressed.match(/^#{1,6}\s+.+$/mg) ?? []).length;
  if (origHeadings !== compHeadings) {
    errors.push(`Heading count: ${origHeadings} → ${compHeadings}`);
  }

  const origFields = (original.match(/^- \*\*[\w-]+\*\*:/mg) ?? []).length;
  const compFields = (compressed.match(/^- \*\*[\w-]+\*\*:/mg) ?? []).length;
  if (origFields !== compFields) {
    errors.push(`MML field count: ${origFields} → ${compFields}`);
  }

  const origBlocks = extractFencedBlocks(original);
  const compBlocks = extractFencedBlocks(compressed);
  if (origBlocks.join("\n---\n") !== compBlocks.join("\n---\n")) {
    errors.push("Code blocks modified or removed");
  }

  return errors;
}

function extractFencedBlocks(text: string): string[] {
  const blocks: string[] = [];
  const lines = text.split("\n");
  let inBlock = false;
  let fenceChar = "";
  let fenceLen = 0;
  let current: string[] = [];
  for (const line of lines) {
    const m = line.match(/^(`{3,}|~{3,})/);
    if (!inBlock && m) {
      inBlock = true; fenceChar = m[1][0]; fenceLen = m[1].length; current = [line];
    } else if (inBlock && m && m[1][0] === fenceChar && m[1].length >= fenceLen) {
      current.push(line); blocks.push(current.join("\n")); inBlock = false; current = [];
    } else if (inBlock) {
      current.push(line);
    }
  }
  return blocks;
}

function writeCompressedFile(filePath: string, original: string, compressed: string): void {
  // Store backup outside .memorykit/ so retrieve_context doesn't load it as a memory entry
  const backupDir = join(tmpdir(), "memorykit-compress-backups");
  mkdirSync(backupDir, { recursive: true });
  const stem = basename(filePath).replace(/\.md$/, "");
  writeFileSync(join(backupDir, `${stem}.original.md`), original, "utf-8");
  writeFileSync(filePath, compressed, "utf-8");
}

/**
 * Group files by layer
 */
function groupFilesByLayer(files: any[]): Record<string, any[]> {
  const grouped: Record<string, any[]> = {};
  for (const file of files) {
    const layer = file.layer;
    if (!grouped[layer]) grouped[layer] = [];
    grouped[layer].push(file);
  }
  return grouped;
}

/**
 * Create CLI program
 */
export function createCLI(): Command {
  const program = new Command();

  program
    .name("memorykit")
    .description("Cognitive memory for AI coding assistants")
    .version(_pkg.version);

  program
    .command("init")
    .description("Initialize .memorykit/ directory")
    .option("--global", "Initialize global memory (~/.memorykit/)")
    .action(initCommand);

  program
    .command("status")
    .description("Show memory statistics")
    .action(statusCommand);

  program
    .command("consolidate")
    .description("Run memory maintenance")
    .option("--scope <scope>", "project, global, or all", "project")
    .option("--dry-run", "Report without modifying")
    .action(consolidateCommand);

  program
    .command("compress")
    .description("Compress memory files to reduce input token cost")
    .option("--scope <scope>", "project, global, or all", "project")
    .option("--dry-run", "Show what would be compressed without modifying")
    .action(compressCommand);

  program
    .command("statusline")
    .description("Output one-line badge for Claude Code statusline")
    .action(statuslineCommand);

  return program;
}
