/**
 * MCP Tool: retrieve_context
 */

import type { RetrieveOptions } from "../types/memory.js";
import { retrieveContext } from "../memory/retrieve.js";
import { validateInput, RetrieveContextSchema } from "../types/validation.js";

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

/**
 * Format large numbers with thousands separator (54321 → 54k)
 */
function formatTokens(tokens: number): string {
  if (tokens >= 1000) {
    return `${Math.round(tokens / 1000)}k`;
  }
  return tokens.toString();
}

export async function handleRetrieveContext(args: unknown): Promise<any> {
  const v = validateInput(RetrieveContextSchema, args);
  if (!v.success) {
    return {
      content: [{ type: "text", text: `Validation error: ${v.error}` }],
      isError: true,
    };
  }
  const options: RetrieveOptions = {
    max_tokens: v.data.max_tokens,
    layers: v.data.layers as RetrieveOptions["layers"],
    scope: v.data.scope as RetrieveOptions["scope"],
  };
  const result = await retrieveContext(v.data.query, options);

  // Format ROI banner with clean markdown (no box-drawing chars)
  const estimatedNote = result.roi_stats.is_estimated ? " *(estimated)*" : "";
  const roiBanner = [
    `🧠 **MemoryKit**: Found ${result.entries_returned} relevant memories (~${formatTokens(result.token_estimate)} tokens)`,
    `💰 **Estimated savings**: ~${formatTokens(result.roi_stats.tokens_saved)} tokens, ~${result.roi_stats.tool_calls_saved} tool calls${estimatedNote}`,
    `📈 **Efficiency**: ${result.roi_stats.efficiency_percent}%`,
  ].join("\n");

  return {
    content: [
      {
        type: "text",
        text: `${roiBanner}\n${result.context}`,
      },
    ],
  };
}
