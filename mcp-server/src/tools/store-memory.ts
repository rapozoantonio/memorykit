/**
 * MCP Tool: store_memory
 */

import type { StoreOptions } from "../types/memory.js";
import { storeMemory } from "../memory/store.js";

export const storeMemoryTool = {
  name: "store_memory",
  description:
    "Store a new memory entry with automatic importance scoring and layer routing",
  inputSchema: {
    type: "object",
    properties: {
      content: {
        type: "string",
        description: "The memory content to store",
      },
      tags: {
        type: "array",
        items: { type: "string" },
        description: "Categorization tags (auto-detected if omitted)",
      },
      layer: {
        type: "string",
        enum: ["working", "facts", "episodes", "procedures"],
        description: "Override target layer (auto-determined if omitted)",
      },
      scope: {
        type: "string",
        enum: ["project", "global"],
        default: "project",
        description: "Storage scope",
      },
      file_hint: {
        type: "string",
        description: 'Suggest target file within layer (e.g., "technology")',
      },
    },
    required: ["content"],
  },
};

export async function handleStoreMemory(args: any): Promise<any> {
  const options: StoreOptions = {
    tags: args.tags,
    layer: args.layer,
    scope: args.scope,
    file_hint: args.file_hint,
  };

  const result = await storeMemory(args.content, options);

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(result, null, 2),
      },
    ],
  };
}
