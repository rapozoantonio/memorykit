#!/usr/bin/env node

/**
 * MemoryKit Entry Point
 * Determines whether to run CLI commands or start MCP server
 */

import { createCLI } from "./cli.js";
import { startServer } from "./server.js";

async function main() {
  const args = process.argv.slice(2);

  // If CLI command provided, run CLI
  if (
    args.length > 0 &&
    [
      "init",
      "status",
      "consolidate",
      "-h",
      "--help",
      "-V",
      "--version",
    ].includes(args[0])
  ) {
    const program = createCLI();
    await program.parseAsync(process.argv);
  } else {
    // No CLI command, start MCP server
    await startServer();
  }
}

main().catch((error) => {
  console.error("[MemoryKit] Fatal error:", error);
  process.exit(1);
});
