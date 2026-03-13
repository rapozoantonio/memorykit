/**
 * MCP Tool: consolidate
 */

import type { ConsolidateOptions } from "../types/memory.js";
import { consolidateMemory } from "../memory/consolidate.js";
import { validateInput, ConsolidateSchema } from "../types/validation.js";

export const consolidateTool = {
  name: "consolidate",
  description: "Trigger memory maintenance (prune, promote, compact)",
  inputSchema: {
    type: "object",
    properties: {
      scope: {
        type: "string",
        enum: ["project", "global", "all"],
        default: "project",
        description: "Which scope to consolidate",
      },
      dry_run: {
        type: "boolean",
        description: "Report changes without modifying files",
      },
    },
  },
};

export async function handleConsolidate(args: unknown): Promise<any> {
  const v = validateInput(ConsolidateSchema, args);
  if (!v.success) {
    return {
      content: [{ type: "text", text: `Validation error: ${v.error}` }],
      isError: true,
    };
  }
  const options: ConsolidateOptions = {
    scope: v.data.scope,
    dry_run: v.data.dry_run,
  };
  const result = await consolidateMemory(options);

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(result, null, 2),
      },
    ],
  };
}
