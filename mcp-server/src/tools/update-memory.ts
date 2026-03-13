/**
 * MCP Tool: update_memory
 */

import type { UpdateOptions } from "../types/memory.js";
import { updateMemory } from "../memory/update.js";
import { validateInput, UpdateMemorySchema } from "../types/validation.js";

export const updateMemoryTool = {
  name: "update_memory",
  description: "Modify an existing memory entry",
  inputSchema: {
    type: "object",
    properties: {
      entry_id: {
        type: "string",
        description: "ID of entry to update",
      },
      content: {
        type: "string",
        description: "New content (replaces existing)",
      },
      tags: {
        type: "array",
        items: { type: "string" },
        description: "Updated tags",
      },
      importance: {
        type: "number",
        description: "Manual importance override (0.0-1.0)",
      },
    },
    required: ["entry_id"],
  },
};

export async function handleUpdateMemory(args: unknown): Promise<any> {
  const v = validateInput(UpdateMemorySchema, args);
  if (!v.success) {
    return {
      content: [{ type: "text", text: `Validation error: ${v.error}` }],
      isError: true,
    };
  }
  const updates: UpdateOptions = {
    what: v.data.content,
    tags: v.data.tags,
    importance: v.data.importance,
  };
  const result = await updateMemory(v.data.entry_id, updates);

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(result, null, 2),
      },
    ],
  };
}
