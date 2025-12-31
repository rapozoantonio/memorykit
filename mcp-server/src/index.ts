#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  ListToolsRequestSchema,
  CallToolRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { ProcessManager } from "./process-manager.js";
import { MemoryKitApiClient } from "./api-client.js";
import { registerTools } from "./tools/index.js";

const API_KEY = process.env.MEMORYKIT_API_KEY || "mcp-local-key";
const USE_DOCKER = process.env.MEMORYKIT_USE_DOCKER !== "false";
const EXTERNAL_API = process.env.MEMORYKIT_EXTERNAL_API === "true";

async function main() {
  // Start .NET API process
  const processManager = new ProcessManager({
    apiKey: API_KEY,
    port: 5555,
    useDocker: USE_DOCKER,
    externalApi: EXTERNAL_API,
  });

  try {
    await processManager.start();

    // Create API client
    const apiClient = new MemoryKitApiClient(
      processManager.getBaseUrl(),
      processManager.getApiKey()
    );

    // Create MCP server
    const server = new Server(
      {
        name: "memorykit-mcp-server",
        version: "0.1.0",
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    // Register tool handlers
    registerTools(server, apiClient);

    // Handle graceful shutdown
    const shutdown = async () => {
      console.error("\n[MCP] Shutting down...");
      await processManager.stop();
      process.exit(0);
    };

    process.on("SIGINT", shutdown);
    process.on("SIGTERM", shutdown);

    // Start MCP server with stdio transport
    const transport = new StdioServerTransport();
    await server.connect(transport);

    console.error("[MCP] Server ready and listening on stdio");
  } catch (error) {
    console.error("[MCP] Fatal error:", error);
    await processManager.stop();
    process.exit(1);
  }
}

main();
