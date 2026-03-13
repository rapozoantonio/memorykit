/**
 * MCP Tool: store_memory
 */

import type { StoreOptions } from "../types/memory.js";
import { storeMemory } from "../memory/store.js";
import { validateInput, StoreMemorySchema } from "../types/validation.js";

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
      acquisition_context: {
        type: "object",
        description: "Cost to acquire this knowledge (for ROI tracking)",
        properties: {
          tokens_consumed: {
            type: "number",
            description: "Total tokens spent discovering this knowledge",
          },
          tool_calls: {
            type: "number",
            description:
              "Number of tool calls (searches, reads) that produced this",
          },
        },
        required: ["tokens_consumed", "tool_calls"],
      },
    },
    required: ["content"],
  },
};

export async function handleStoreMemory(args: unknown): Promise<any> {
  const v = validateInput(StoreMemorySchema, args);
  if (!v.success) {
    return {
      content: [{ type: "text", text: `Validation error: ${v.error}` }],
      isError: true,
    };
  }
  const options: StoreOptions = {
    tags: v.data.tags,
    layer: v.data.layer as StoreOptions["layer"],
    scope: v.data.scope as StoreOptions["scope"],
    file_hint: v.data.file_hint,
    acquisition_context: v.data.acquisition_context,
  };
  const result = await storeMemory(v.data.content, options);

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(result, null, 2),
      },
    ],
  };
}
