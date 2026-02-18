/**
 * MCP Tool: forget_memory
 */

import { forgetMemory } from "../memory/forget.js";

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

export async function handleForgetMemory(args: any): Promise<any> {
  const result = await forgetMemory(args.entry_id);

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(result, null, 2),
      },
    ],
  };
}
