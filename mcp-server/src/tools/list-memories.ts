/**
 * MCP Tool: list_memories
 */

import type { ListResult, MemoryLayer } from "../types/memory.js";
import { MemoryLayer as Layer } from "../types/memory.js";
import { listMemoryFiles } from "../storage/file-manager.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
  isProjectInitialized,
  isGlobalInitialized,
} from "../storage/scope-resolver.js";

export const listMemoriesTool = {
  name: "list_memories",
  description: "Browse memory structure and statistics",
  inputSchema: {
    type: "object",
    properties: {
      scope: {
        type: "string",
        enum: ["all", "project", "global"],
        default: "all",
        description: "Which scope to list",
      },
      layer: {
        type: "string",
        enum: ["working", "facts", "episodes", "procedures"],
        description: "Filter by specific layer",
      },
    },
  },
};

export async function handleListMemories(args: any): Promise<any> {
  const result: ListResult = {};

  // List project memories
  if (args.scope !== "global" && isProjectInitialized()) {
    const projectFiles = await listMemoryFiles(resolveProjectRoot());
    result.project = groupByLayer(projectFiles, args.layer);
  }

  // List global memories
  if (args.scope !== "project" && isGlobalInitialized()) {
    const globalFiles = await listMemoryFiles(resolveGlobalRoot());
    result.global = groupByLayer(globalFiles, args.layer);
  }

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(result, null, 2),
      },
    ],
  };
}

function groupByLayer(files: any[], filterLayer?: string) {
  const grouped: any = {};

  const layers: MemoryLayer[] = [
    Layer.Working,
    Layer.Facts,
    Layer.Episodes,
    Layer.Procedures,
  ];

  for (const layer of layers) {
    if (filterLayer && layer !== filterLayer) continue;

    const layerFiles = files.filter((f) => f.layer === layer);

    if (layerFiles.length > 0) {
      grouped[layer] = {
        files: layerFiles.map((f) => f.filename),
        entry_count: layerFiles.reduce((sum, f) => sum + f.entryCount, 0),
      };
    }
  }

  return grouped;
}
