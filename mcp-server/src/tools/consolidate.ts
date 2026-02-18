/**
 * MCP Tool: consolidate
 */

import type { ConsolidateOptions } from "../types/memory.js";
import { consolidateMemory } from "../memory/consolidate.js";

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

export async function handleConsolidate(args: any): Promise<any> {
  const options: ConsolidateOptions = {
    scope: args.scope,
    dry_run: args.dry_run,
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
