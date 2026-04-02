/**
 * CLI init command tests
 * Verifies memorykit init creates both .vscode/mcp.json and .mcp.json
 */

import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { mkdtempSync, rmSync, readFileSync, existsSync, mkdirSync } from "fs";
import { join } from "path";
import { tmpdir } from "os";
import { initCommand } from "../cli.js";

describe("CLI init command", () => {
  let testDir: string;
  const originalEnv = process.env.MEMORYKIT_PROJECT;

  beforeEach(() => {
    testDir = mkdtempSync(join(tmpdir(), "memorykit-cli-test-"));
    process.env.MEMORYKIT_PROJECT = testDir;
  });

  afterEach(() => {
    process.env.MEMORYKIT_PROJECT = originalEnv;
    try {
      rmSync(testDir, { recursive: true, force: true });
    } catch (err) {
      console.error("Failed to clean up test directory:", err);
    }
  });

  it("should create .vscode/mcp.json for GitHub Copilot", async () => {
    await initCommand({});

    const vscodeConfig = join(testDir, ".vscode", "mcp.json");
    expect(existsSync(vscodeConfig)).toBe(true);

    const content = JSON.parse(readFileSync(vscodeConfig, "utf-8"));
    expect(content.servers.memorykit).toBeDefined();
    expect(content.servers.memorykit.type).toBe("stdio");
    expect(content.servers.memorykit.command).toBe("memorykit");
    expect(content.servers.memorykit.env.MEMORYKIT_PROJECT).toBe(
      "${workspaceFolder}",
    );
  });

  it("should create .mcp.json for Claude Code", async () => {
    await initCommand({});

    const claudeConfig = join(testDir, ".mcp.json");
    expect(existsSync(claudeConfig)).toBe(true);

    const content = JSON.parse(readFileSync(claudeConfig, "utf-8"));
    expect(content.mcpServers.memorykit).toBeDefined();
    expect(content.mcpServers.memorykit.command).toBe("memorykit");
    expect(content.mcpServers.memorykit.args).toEqual([]);
    expect(content.mcpServers.memorykit.env).toEqual({});
  });

  it("should create both config files in one run", async () => {
    await initCommand({});

    expect(existsSync(join(testDir, ".vscode", "mcp.json"))).toBe(true);
    expect(existsSync(join(testDir, ".mcp.json"))).toBe(true);
  });

  it("should skip existing VS Code config", async () => {
    await initCommand({});

    const vscodeConfig = join(testDir, ".vscode", "mcp.json");
    const originalContent = readFileSync(vscodeConfig, "utf-8");

    // Run again
    await initCommand({});

    // Should not overwrite
    const newContent = readFileSync(vscodeConfig, "utf-8");
    expect(newContent).toBe(originalContent);
  });

  it("should skip existing Claude Code config", async () => {
    await initCommand({});

    const claudeConfig = join(testDir, ".mcp.json");
    const originalContent = readFileSync(claudeConfig, "utf-8");

    // Run again
    await initCommand({});

    // Should not overwrite
    const newContent = readFileSync(claudeConfig, "utf-8");
    expect(newContent).toBe(originalContent);
  });

  it("should not create MCP configs when global flag is used", async () => {
    await initCommand({ global: true });

    expect(existsSync(join(testDir, ".vscode", "mcp.json"))).toBe(false);
    expect(existsSync(join(testDir, ".mcp.json"))).toBe(false);
  });

  it("should create MCP configs even if memory directory already exists", async () => {
    // First init creates memory directory
    await initCommand({});

    // Delete the MCP configs
    rmSync(join(testDir, ".vscode"), { recursive: true, force: true });
    rmSync(join(testDir, ".mcp.json"), { force: true });

    // Second init should still create MCP configs
    await initCommand({});

    expect(existsSync(join(testDir, ".vscode", "mcp.json"))).toBe(true);
    expect(existsSync(join(testDir, ".mcp.json"))).toBe(true);
  });

  it("should create CLAUDE.md with MemoryKit instructions", async () => {
    await initCommand({});

    const claudeMd = join(testDir, "CLAUDE.md");
    expect(existsSync(claudeMd)).toBe(true);

    const content = readFileSync(claudeMd, "utf-8");
    expect(content).toContain("# Project Memory Instructions");
    expect(content).toContain("MemoryKit");
    expect(content).toContain("retrieve_context");
    expect(content).toContain("store_memory");
    expect(content).toContain("## Before Starting Any Task");
    expect(content).toContain("## When Completing Work");
  });

  it("should create .github/copilot-instructions.md with MemoryKit section", async () => {
    await initCommand({});

    const copilotInstructions = join(
      testDir,
      ".github",
      "copilot-instructions.md",
    );
    expect(existsSync(copilotInstructions)).toBe(true);

    const content = readFileSync(copilotInstructions, "utf-8");
    expect(content).toContain("## Memory System (MemoryKit)");
    expect(content).toContain("retrieve_context");
    expect(content).toContain("store_memory");
  });

  it("should append to existing CLAUDE.md without MemoryKit section", async () => {
    const claudeMd = join(testDir, "CLAUDE.md");
    const existingContent = "# My Project\n\nExisting instructions here.\n";

    // Create pre-existing CLAUDE.md
    mkdirSync(testDir, { recursive: true });
    rmSync(claudeMd, { force: true });
    const { writeFileSync } = await import("fs");
    writeFileSync(claudeMd, existingContent, "utf-8");

    await initCommand({});

    const newContent = readFileSync(claudeMd, "utf-8");
    expect(newContent).toContain("# My Project");
    expect(newContent).toContain("Existing instructions here");
    expect(newContent).toContain("## Before Starting Any Task");
    expect(newContent.indexOf("Existing instructions")).toBeLessThan(
      newContent.indexOf("## Before Starting Any Task"),
    );
  });

  it("should append to existing copilot-instructions.md without MemoryKit section", async () => {
    const githubDir = join(testDir, ".github");
    const copilotInstructions = join(githubDir, "copilot-instructions.md");
    const existingContent = "# Existing Copilot Rules\n\nSome rules here.\n";

    // Create pre-existing file
    const { mkdirSync, writeFileSync } = await import("fs");
    mkdirSync(githubDir, { recursive: true });
    writeFileSync(copilotInstructions, existingContent, "utf-8");

    await initCommand({});

    const newContent = readFileSync(copilotInstructions, "utf-8");
    expect(newContent).toContain("# Existing Copilot Rules");
    expect(newContent).toContain("Some rules here");
    expect(newContent).toContain("## Memory System (MemoryKit)");
    expect(newContent.indexOf("Some rules here")).toBeLessThan(
      newContent.indexOf("## Memory System (MemoryKit)"),
    );
  });

  it("should skip CLAUDE.md if MemoryKit section already exists", async () => {
    const claudeMd = join(testDir, "CLAUDE.md");
    const existingContent = `# My Project

## Before Starting Any Task

Already has MemoryKit stuff.
`;

    const { writeFileSync } = await import("fs");
    writeFileSync(claudeMd, existingContent, "utf-8");

    await initCommand({});

    const newContent = readFileSync(claudeMd, "utf-8");
    // Should not duplicate
    expect(newContent).toBe(existingContent);
  });

  it("should skip copilot-instructions.md if MemoryKit section already exists", async () => {
    const githubDir = join(testDir, ".github");
    const copilotInstructions = join(githubDir, "copilot-instructions.md");
    const existingContent = `# Rules

## Memory System (MemoryKit)

Already configured.
`;

    const { mkdirSync, writeFileSync } = await import("fs");
    mkdirSync(githubDir, { recursive: true });
    writeFileSync(copilotInstructions, existingContent, "utf-8");

    await initCommand({});

    const newContent = readFileSync(copilotInstructions, "utf-8");
    // Should not duplicate
    expect(newContent).toBe(existingContent);
  });

  it("should not create AI instruction files when global flag is used", async () => {
    await initCommand({ global: true });

    expect(existsSync(join(testDir, "CLAUDE.md"))).toBe(false);
    expect(
      existsSync(join(testDir, ".github", "copilot-instructions.md")),
    ).toBe(false);
  });
});
