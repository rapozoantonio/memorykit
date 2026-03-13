/**
 * MCP Tool: forget_memory
 */

import { forgetMemory } from "../memory/forget.js";
import { validateInput, ForgetMemorySchema } from "../types/validation.js";

export const forgetMemoryTool = {
  name: "forget_memory",
  description: "Remove a memory entry",
  inputSchema: {
    type: "object",
    properties: {
      entry_id: {
        type: "string",
        description: "ID of entry to remove",
      },
    },
    required: ["entry_id"],
  },
};

export async function handleForgetMemory(args: unknown): Promise<any> {
  const v = validateInput(ForgetMemorySchema, args);
  if (!v.success) {
    return {
      content: [{ type: "text", text: `Validation error: ${v.error}` }],
      isError: true,
    };
  }
  const result = await forgetMemory(v.data.entry_id);

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(result, null, 2),
      },
    ],
  };
}
