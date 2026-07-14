/**
 * MCP Tool: list_memories
 */

import type { ListResult, MemoryLayer } from "../types/memory.js";
import { MemoryLayer as Layer } from "../types/memory.js";
import { listMemoryFiles, readMemoryFile } from "../storage/file-manager.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
  isProjectInitialized,
  isGlobalInitialized,
} from "../storage/scope-resolver.js";
import { validateInput, ListMemoriesSchema } from "../types/validation.js";

export const listMemoriesTool = {
  name: "list_memories",
  description:
    "Browse memory structure and statistics. Use include_content:true to see entry content. Use tags to filter by tag.",
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
      tags: {
        type: "array",
        items: { type: "string" },
        description: "Return only entries that match any of these tags",
      },
      include_content: {
        type: "boolean",
        description:
          "Include entry title and content in results (default: false, counts only)",
      },
      max_entries: {
        type: "number",
        description:
          "Maximum entries to return when include_content is true (default: 50)",
      },
    },
  },
};

export async function handleListMemories(args: unknown): Promise<any> {
  const v = validateInput(ListMemoriesSchema, args);
  if (!v.success) {
    return {
      content: [{ type: "text", text: `Validation error: ${v.error}` }],
      isError: true,
    };
  }
  const result: ListResult = {};
  const filterTags = v.data.tags;
  const includeContent = v.data.include_content ?? false;
  const maxEntries = v.data.max_entries ?? 50;

  if (v.data.scope !== "global" && isProjectInitialized()) {
    const projectFiles = await listMemoryFiles(resolveProjectRoot());
    result.project = await groupByLayer(
      projectFiles,
      v.data.layer,
      filterTags,
      includeContent,
      maxEntries,
    );
  }
  if (v.data.scope !== "project" && isGlobalInitialized()) {
    const globalFiles = await listMemoryFiles(resolveGlobalRoot());
    result.global = await groupByLayer(
      globalFiles,
      v.data.layer,
      filterTags,
      includeContent,
      maxEntries,
    );
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

async function groupByLayer(
  files: any[],
  filterLayer?: string,
  filterTags?: string[],
  includeContent?: boolean,
  maxEntries?: number,
) {
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
      if (!includeContent && !filterTags?.length) {
        // Fast path: counts only (original behaviour)
        grouped[layer] = {
          files: layerFiles.map((f) => f.filename),
          entry_count: layerFiles.reduce((sum, f) => sum + f.entryCount, 0),
        };
      } else {
        // Read entries in parallel for content/tag filtering
        const fileReads = await Promise.all(
          layerFiles.map((f) => readMemoryFile(f.path)),
        );
        const allEntries: any[] = [];
        let truncated = false;

        for (const entries of fileReads) {
          for (const entry of entries) {
            // Tag filter: keep if any requested tag matches
            if (filterTags?.length) {
              const entryTagSet = new Set(
                entry.tags.map((t: string) => t.toLowerCase()),
              );
              const hasMatch = filterTags.some((t) =>
                entryTagSet.has(t.toLowerCase()),
              );
              if (!hasMatch) continue;
            }

            // Size cap when include_content is true
            if (
              includeContent &&
              maxEntries &&
              allEntries.length >= maxEntries
            ) {
              truncated = true;
              break;
            }

            allEntries.push(
              includeContent
                ? {
                    id: entry.id,
                    title: entry.title,
                    what: entry.what,
                    tags: entry.tags,
                    importance: entry.importance,
                    created: entry.created,
                    layer: entry.layer,
                  }
                : {
                    id: entry.id,
                    title: entry.title,
                    tags: entry.tags,
                    importance: entry.importance,
                  },
            );
          }
          if (truncated) break;
        }
        if (allEntries.length > 0) {
          grouped[layer] = {
            entry_count: allEntries.length,
            entries: allEntries,
            truncated,
          };
        }
      }
    }
  }

  return grouped;
}
