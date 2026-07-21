/**
 * CLI commands - init, status, consolidate
 */

import { Command } from "commander";
import { mkdirSync, writeFileSync, existsSync, readFileSync } from "fs";
import { join, dirname } from "path";
import { homedir } from "os";
import { fileURLToPath } from "url";

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

/**
 * Initialize .memorykit/ directory structure
 */
export async function initCommand(options: {
  global?: boolean;
}): Promise<void> {
  const root = options.global ? resolveGlobalRoot() : resolveProjectRoot();

  // Create memory directory structure if it doesn't exist
  if (existsSync(root)) {
    console.log(`✅ Memory already initialized at: ${root}`);
  } else {
    // Create directory structure
    mkdirSync(root, { recursive: true });
    mkdirSync(join(root, MemoryLayer.Working), { recursive: true });
    mkdirSync(join(root, MemoryLayer.Facts), { recursive: true });
    mkdirSync(join(root, MemoryLayer.Episodes), { recursive: true });
    mkdirSync(join(root, MemoryLayer.Procedures), { recursive: true });

    // Create config file
    const configPath = join(root, "memorykit.yaml");
    const configContent = stringifyYaml(getDefaultConfig());
    writeFileSync(configPath, configContent, "utf-8");

    // Create session.md template
    const sessionPath = join(root, MemoryLayer.Working, "session.md");
    const sessionContent = `# Working Memory\n\nCurrent session context and active tasks.\n\n---\n`;
    writeFileSync(sessionPath, sessionContent, "utf-8");

    // Create .gitkeep files
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

  // Create MCP config files (independent of memory directory existence)
  if (!options.global) {
    // Create .vscode/mcp.json for workspace config
    const workingDir = getWorkingDirectory();
    const vscodeDir = join(workingDir, ".vscode");
    const mcpConfigPath = join(vscodeDir, "mcp.json");

    if (!existsSync(mcpConfigPath)) {
      mkdirSync(vscodeDir, { recursive: true });

      const mcpConfig = {
        servers: {
          memorykit: {
            type: "stdio",
            command: "memorykit",
            env: {
              MEMORYKIT_PROJECT: "${workspaceFolder}",
            },
          },
        },
      };

      writeFileSync(mcpConfigPath, JSON.stringify(mcpConfig, null, 2), "utf-8");

      console.log(`\n✅ Created VS Code Copilot config: ${mcpConfigPath}`);
      console.log(`💡 Reload VS Code for changes to take effect`);
    } else {
      console.log(`\n⚠️  .vscode/mcp.json already exists, skipping`);
    }

    // Create .mcp.json for Claude Code (project-scoped MCP config)
    const claudeMcpConfigPath = join(workingDir, ".mcp.json");

    if (!existsSync(claudeMcpConfigPath)) {
      const claudeMcpConfig = {
        mcpServers: {
          memorykit: {
            command: "memorykit",
            alwaysLoad: true,
            args: [],
            env: {},
          },
        },
      };

      writeFileSync(
        claudeMcpConfigPath,
        JSON.stringify(claudeMcpConfig, null, 2),
        "utf-8",
      );

      console.log(`\n✅ Created Claude Code config: ${claudeMcpConfigPath}`);
      console.log(`💡 Claude Code will auto-detect this server in the project`);
    } else {
      console.log(`\n⚠️  .mcp.json already exists, skipping`);
    }

    // Create .cursor/mcp.json for Cursor editor (R1)
    const cursorDir = join(workingDir, ".cursor");
    const cursorMcpConfigPath = join(cursorDir, "mcp.json");

    if (!existsSync(cursorMcpConfigPath)) {
      mkdirSync(cursorDir, { recursive: true });

      const cursorMcpConfig = {
        mcpServers: {
          memorykit: {
            command: "memorykit",
            env: {
              MEMORYKIT_PROJECT: "${workspaceFolder}",
            },
          },
        },
      };

      writeFileSync(
        cursorMcpConfigPath,
        JSON.stringify(cursorMcpConfig, null, 2),
        "utf-8",
      );

      console.log(`\n✅ Created Cursor config: ${cursorMcpConfigPath}`);
      console.log(`💡 Restart Cursor for changes to take effect`);
    } else {
      console.log(`\n⚠️  .cursor/mcp.json already exists, skipping`);
    }

    // Create AGENTS.md — canonical instruction file read by Claude Code, Copilot, and Cursor (R8)
    const agentsMdPath = join(workingDir, "AGENTS.md");
    const agentsMdContent = `## Memory System (MemoryKit)

This project uses **MemoryKit** for persistent memory across AI conversations.

### Before Starting Any Task

Call \`retrieve_context\` with the task description to load relevant past decisions, bugs, and patterns.

### When Completing Work

Use \`store_memory\` to save important knowledge. Choose the right layer:
- **facts**: Architecture decisions, technology choices, constraints
- **episodes**: Bugs fixed, failed approaches, incidents
- **procedures**: Coding patterns, conventions, workflows

Always include the reasoning (WHY), not just the decision (WHAT).

### ROI Tracking

When you spent real effort discovering a fact, pass \`acquisition_context\` to enable exact savings measurement instead of estimates:
- \`tokens_consumed\`: approximate tokens used discovering this fact
- \`tool_calls\`: number of searches or file reads made

Example: \`store_memory({ content: "...", acquisition_context: { tokens_consumed: 800, tool_calls: 3 } })\`

### Handling Store Rejections

If \`store_memory\` returns \`stored: false\`, read the \`suggestion\` field:
- **Near-duplicate**: call \`update_memory\` with the returned \`entry_id\` — do not create a duplicate entry
- **Importance floor**: content was too routine — no action needed
- If \`stored: true\` with a \`warning\`: a contradiction was detected — update the conflicting entry if it is stale
`;

    if (!existsSync(agentsMdPath)) {
      writeFileSync(agentsMdPath, agentsMdContent, "utf-8");
      console.log(`\n✅ Created agent instructions: ${agentsMdPath}`);
      console.log(`💡 Read by Claude Code, GitHub Copilot, and Cursor`);
    } else {
      const existingAgentsContent = readFileSync(agentsMdPath, "utf-8");
      if (!existingAgentsContent.includes("## Memory System (MemoryKit)")) {
        writeFileSync(
          agentsMdPath,
          existingAgentsContent + "\n" + agentsMdContent,
          "utf-8",
        );
        console.log(`\n✅ Added MemoryKit section to: ${agentsMdPath}`);
      } else {
        console.log(
          `\n⚠️  AGENTS.md already has MemoryKit instructions, skipping`,
        );
      }
    }

    // Create CLAUDE.md for Claude Code — thin import of AGENTS.md (R8)
    const claudeMdPath = join(workingDir, "CLAUDE.md");
    const claudeMdContent = `@AGENTS.md\n`;

    if (!existsSync(claudeMdPath)) {
      writeFileSync(claudeMdPath, claudeMdContent, "utf-8");
      console.log(`\n✅ Created Claude Code instructions: ${claudeMdPath}`);
    } else {
      // Check if AGENTS.md import already exists
      const existingContent = readFileSync(claudeMdPath, "utf-8");
      if (!existingContent.includes("@AGENTS.md")) {
        writeFileSync(
          claudeMdPath,
          existingContent + "\n\n" + claudeMdContent,
          "utf-8",
        );
        console.log(`\n✅ Added MemoryKit section to: ${claudeMdPath}`);
      } else {
        console.log(
          `\n⚠️  CLAUDE.md already has MemoryKit instructions, skipping`,
        );
      }
    }

    // Create or append to .github/copilot-instructions.md
    const githubDir = join(workingDir, ".github");
    const copilotInstructionsPath = join(githubDir, "copilot-instructions.md");

    const memoryKitInstructions = `
## Memory System (MemoryKit)

This project uses MemoryKit for persistent memory across conversations.

### Before Starting Any Task
- Call \`retrieve_context\` with the task description to check for relevant past decisions, bugs, or patterns

### When Completing Work
- Use \`store_memory\` to save architectural decisions (facts layer)
- Use \`store_memory\` to record bugs and fixes (episodes layer)
- Use \`store_memory\` to document patterns and conventions (procedures layer)
- Always include the reasoning (WHY), not just the decision (WHAT)

### ROI Tracking
Pass \`acquisition_context\` when you spent real effort discovering a fact — this enables exact savings measurement:
\`store_memory({ content: "...", acquisition_context: { tokens_consumed: 800, tool_calls: 3 } })\`
\`tokens_consumed\` ≈ tokens used; \`tool_calls\` = number of searches/reads made.

### Handling Store Rejections
If \`store_memory\` returns \`stored: false\`, check the \`suggestion\` field:
- **Near-duplicate**: call \`update_memory\` with the returned \`entry_id\` — do not create a duplicate
- **Importance floor**: content was too routine — no action needed
- \`stored: true\` with \`warning\`: contradiction detected — update the conflicting entry if stale
`;

    if (!existsSync(copilotInstructionsPath)) {
      mkdirSync(githubDir, { recursive: true });
      writeFileSync(
        copilotInstructionsPath,
        memoryKitInstructions.trim() + "\n",
        "utf-8",
      );
      console.log(
        `\n✅ Created Copilot instructions: ${copilotInstructionsPath}`,
      );
    } else {
      // Check if MemoryKit section already exists
      const existingContent = readFileSync(copilotInstructionsPath, "utf-8");
      if (!existingContent.includes("## Memory System (MemoryKit)")) {
        // Append to existing file
        writeFileSync(
          copilotInstructionsPath,
          existingContent + "\n" + memoryKitInstructions,
          "utf-8",
        );
        console.log(
          `\n✅ Added MemoryKit section to: ${copilotInstructionsPath}`,
        );
      } else {
        console.log(
          `\n⚠️  MemoryKit instructions already in copilot-instructions.md, skipping`,
        );
      }
    }

    // .claude/settings.local.json — SessionStart hook for guaranteed retrieve_context
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
      writeFileSync(
        settingsLocalPath,
        JSON.stringify({ hooks: { SessionStart: [memorykitHook] } }, null, 2),
        "utf-8",
      );
      console.log(`\n✅ Created Claude Code hooks: ${settingsLocalPath}`);
    } else {
      try {
        const existing = JSON.parse(readFileSync(settingsLocalPath, "utf-8"));
        const hasHook = existing.hooks?.SessionStart?.some((h: any) =>
          h.hooks?.some(
            (hh: any) =>
              hh.server === "memorykit" && hh.tool === "retrieve_context",
          ),
        );
        if (!hasHook) {
          existing.hooks = existing.hooks ?? {};
          existing.hooks.SessionStart = existing.hooks.SessionStart ?? [];
          existing.hooks.SessionStart.push(memorykitHook);
          writeFileSync(
            settingsLocalPath,
            JSON.stringify(existing, null, 2),
            "utf-8",
          );
          console.log(`\n✅ Added MemoryKit hook to: ${settingsLocalPath}`);
        } else {
          console.log(
            `\n⚠️  MemoryKit hook already in settings.local.json, skipping`,
          );
        }
      } catch {
        console.log(
          `\n⚠️  .claude/settings.local.json exists but couldn't be parsed, skipping`,
        );
      }
    }

    // .claude/skills/recall/SKILL.md — /recall slash command
    const recallSkillDir = join(claudeSettingsDir, "skills", "recall");
    const recallSkillPath = join(recallSkillDir, "SKILL.md");
    if (!existsSync(recallSkillPath)) {
      mkdirSync(recallSkillDir, { recursive: true });
      writeFileSync(
        recallSkillPath,
        `---
name: recall
description: Retrieve relevant memories from memorykit for the current task or topic. Use when starting a task, switching context, or when the user asks about past decisions, patterns, or bugs.
allowed-tools: mcp__memorykit__retrieve_context
---

Call retrieve_context with the specific task or topic as the query — be precise.
A narrow query ("auth token refresh bug") returns better results than a broad one ("authentication").
If an argument is provided (e.g. /recall auth module), use it directly as the query.
Otherwise, derive the query from what the user is currently trying to do.
Return the results without reformatting.
`,
        "utf-8",
      );
      console.log(`\n✅ Created /recall skill: ${recallSkillPath}`);
    } else {
      console.log(`\n⚠️  /recall skill already exists, skipping`);
    }

    // .claude/skills/save/SKILL.md — /save slash command
    const saveSkillDir = join(claudeSettingsDir, "skills", "save");
    const saveSkillPath = join(saveSkillDir, "SKILL.md");
    if (!existsSync(saveSkillPath)) {
      mkdirSync(saveSkillDir, { recursive: true });
      writeFileSync(
        saveSkillPath,
        `---
name: save
description: Save an important discovery, decision, or pattern to memorykit memory. Use after fixing a bug, making an architecture decision, establishing a coding convention, or completing significant work.
allowed-tools: mcp__memorykit__store_memory
---

Call store_memory with precise, self-contained content — write it as if explaining to a
future developer who has no session context. Include the WHY, not just the WHAT.

Choose the correct layer:
- facts: architecture decisions, technology choices, permanent constraints
- episodes: bugs fixed, failed approaches, incidents, root causes
- procedures: coding patterns, conventions, repeatable workflows
- working: active task context (auto-expires after 7 days)

If this discovery took real investigation (multiple tool calls, reading several files),
estimate tokens_consumed and tool_calls and pass them in acquisition_context.
This enables exact ROI measurement on retrieval.
`,
        "utf-8",
      );
      console.log(`\n✅ Created /save skill: ${saveSkillPath}`);
    } else {
      console.log(`\n⚠️  /save skill already exists, skipping`);
    }

    // .claude/rules/memory.md — path-scoped retrieval reminder
    const rulesDir = join(claudeSettingsDir, "rules");
    const rulesPath = join(rulesDir, "memory.md");
    if (!existsSync(rulesPath)) {
      mkdirSync(rulesDir, { recursive: true });
      writeFileSync(
        rulesPath,
        `---
paths:
  - "src/**/*"
  - "*.ts"
  - "*.py"
  - "*.go"
  - "*.rs"
  - "*.java"
  - "*.kt"
---

Before modifying this file, check if retrieve_context has been called for the specific
component or module being changed. Use the filename or module name as the query — not a
generic query. Specific queries return better results.

After discovering a bug root cause or an important constraint in this code, call store_memory.
`,
        "utf-8",
      );
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
    console.log(
      "❌ No memory initialized. Run `memorykit init` to get started.",
    );
    return;
  }

  console.log("📊 MemoryKit Status\n");

  // Project memory stats
  if (projectInitialized) {
    const projectRoot = resolveProjectRoot();
    const projectFiles = await listMemoryFiles(projectRoot);
    const projectEntries = await getTotalEntryCount(projectRoot);

    console.log(`📁 Project Memory: ${projectRoot}`);
    console.log(`   Total entries: ${projectEntries}`);

    const byLayer = groupFilesByLayer(projectFiles);
    for (const [layer, files] of Object.entries(byLayer)) {
      console.log(
        `   ${layer}: ${files.length} files, ${files.reduce((sum, f) => sum + f.entryCount, 0)} entries`,
      );
    }
    console.log("");
  }

  // Global memory stats
  if (globalInitialized) {
    const globalRoot = resolveGlobalRoot();
    const globalFiles = await listMemoryFiles(globalRoot);
    const globalEntries = await getTotalEntryCount(globalRoot);

    console.log(`🌍 Global Memory: ${globalRoot}`);
    console.log(`   Total entries: ${globalEntries}`);

    const byLayer = groupFilesByLayer(globalFiles);
    for (const [layer, files] of Object.entries(byLayer)) {
      console.log(
        `   ${layer}: ${files.length} files, ${files.reduce((sum, f) => sum + f.entryCount, 0)} entries`,
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

  console.log(
    `🧹 Consolidating ${scope} memory${dryRun ? " (dry run)" : ""}...\n`,
  );

  const result = await consolidateMemory({ scope, dry_run: dryRun });

  console.log(`✅ Consolidation complete:`);
  console.log(`   Pruned: ${result.pruned}`);
  console.log(`   Promoted: ${result.promoted}`);
  console.log(`   Compacted: ${result.compacted}`);

  if (result.details.length > 0) {
    console.log(`\nDetails:`);
    for (const action of result.details.slice(0, 10)) {
      console.log(
        `   - ${action.action}: ${action.entry_id} ${action.reason || ""}`,
      );
    }

    if (result.details.length > 10) {
      console.log(`   ... and ${result.details.length - 10} more actions`);
    }
  }
}

/**
 * Group files by layer
 */
function groupFilesByLayer(files: any[]): Record<string, any[]> {
  const grouped: Record<string, any[]> = {};

  for (const file of files) {
    const layer = file.layer;
    if (!grouped[layer]) {
      grouped[layer] = [];
    }
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

  return program;
}
