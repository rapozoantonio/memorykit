/**
 * MCP Tool: retrieve_context
 */

import type { RetrieveOptions } from "../types/memory.js";
import { retrieveContext } from "../memory/retrieve.js";

export const retrieveContextTool = {
  name: "retrieve_context",
  description:
    "Get relevant memory context for a query with intelligent routing",
  inputSchema: {
    type: "object",
    properties: {
      query: {
        type: "string",
        description: "The question or topic to retrieve context for",
      },
      max_tokens: {
        type: "number",
        description: "Token budget override",
      },
      layers: {
        type: "array",
        items: {
          type: "string",
          enum: ["working", "facts", "episodes", "procedures"],
        },
        description: "Restrict to specific layers",
      },
      scope: {
        type: "string",
        enum: ["all", "project", "global"],
        default: "all",
        description: "Search scope",
      },
    },
    required: ["query"],
  },
};

export async function handleRetrieveContext(args: any): Promise<any> {
  const options: RetrieveOptions = {
    max_tokens: args.max_tokens,
    layers: args.layers,
    scope: args.scope,
  };

  const result = await retrieveContext(args.query, options);

  return {
    content: [
      {
        type: "text",
        text: result.context,
      },
    ],
  };
}
