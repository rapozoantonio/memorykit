/**
 * CLI commands - init, status, consolidate
 */

import { Command } from "commander";
import { mkdirSync, writeFileSync, existsSync, readFileSync } from "fs";
import { join } from "path";
import { homedir } from "os";
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

  if (existsSync(root)) {
    console.log(`✅ Memory already initialized at: ${root}`);
    return;
  }

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
    .version("0.1.0");

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
