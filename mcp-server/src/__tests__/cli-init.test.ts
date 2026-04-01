/**
 * CLI init command tests
 * Verifies memorykit init creates both .vscode/mcp.json and .mcp.json
 */

import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { mkdtempSync, rmSync, readFileSync, existsSync } from "fs";
import { join } from "path";
import { tmpdir } from "os";
import { initCommand } from "../cli.js";

describe("CLI init command", () => {
  let testDir: string;
  const originalCwd = process.cwd();

  beforeEach(() => {
    testDir = mkdtempSync(join(tmpdir(), "memorykit-cli-test-"));
    process.chdir(testDir);
  });

  afterEach(() => {
    process.chdir(originalCwd);
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
});
