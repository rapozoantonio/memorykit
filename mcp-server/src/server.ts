/**
 * MCP Server setup and tool registration
 */

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  ListToolsRequestSchema,
  CallToolRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { readFileSync } from "fs";
import { fileURLToPath } from "url";
import { dirname, join } from "path";

// Read version from package.json at runtime — prevents version drift
const _pkgPath = join(
  dirname(fileURLToPath(import.meta.url)),
  "..",
  "package.json",
);
const _pkg = JSON.parse(readFileSync(_pkgPath, "utf-8")) as { version: string };
import { storeMemoryTool, handleStoreMemory } from "./tools/store-memory.js";
import {
  retrieveContextTool,
  handleRetrieveContext,
} from "./tools/retrieve-context.js";
import { updateMemoryTool, handleUpdateMemory } from "./tools/update-memory.js";
import { forgetMemoryTool, handleForgetMemory } from "./tools/forget-memory.js";
import { consolidateTool, handleConsolidate } from "./tools/consolidate.js";
import { listMemoriesTool, handleListMemories } from "./tools/list-memories.js";

/**
 * Create and configure MCP server
 */
export function createServer(): Server {
  const server = new Server(
    {
      name: "memorykit",
      version: _pkg.version,
    },
    {
      capabilities: {
        tools: {},
      },
    },
  );

  // Register tool list handler
  server.setRequestHandler(ListToolsRequestSchema, async () => ({
    tools: [
      storeMemoryTool,
      retrieveContextTool,
      updateMemoryTool,
      forgetMemoryTool,
      consolidateTool,
      listMemoriesTool,
    ],
  }));

  // Register tool call handler
  server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const { name, arguments: args } = request.params;

    if (!args) {
      return {
        content: [
          {
            type: "text",
            text: "Error: No arguments provided",
          },
        ],
        isError: true,
      };
    }

    try {
      switch (name) {
        case "store_memory":
          return await handleStoreMemory(args);

        case "retrieve_context":
          return await handleRetrieveContext(args);

        case "update_memory":
          return await handleUpdateMemory(args);

        case "forget_memory":
          return await handleForgetMemory(args);

        case "consolidate":
          return await handleConsolidate(args);

        case "list_memories":
          return await handleListMemories(args);

        default:
          return {
            content: [
              {
                type: "text",
                text: `Unknown tool: ${name}`,
              },
            ],
            isError: true,
          };
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      console.error(`Error handling tool ${name}:`, error);

      return {
        content: [
          {
            type: "text",
            text: `Error: ${errorMessage}`,
          },
        ],
        isError: true,
      };
    }
  });

  return server;
}

/**
 * Start MCP server with stdio transport
 */
export async function startServer(): Promise<void> {
  const server = createServer();
  const transport = new StdioServerTransport();

  await server.connect(transport);

  console.error("[MemoryKit] MCP server ready");
}
