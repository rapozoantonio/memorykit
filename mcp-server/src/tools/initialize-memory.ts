/**
 * MCP Tool: initialize_memory
 * Creates the MemoryKit directory structure for the current project.
 * Idempotent — safe to call multiple times.
 */

import { mkdirSync, writeFileSync, existsSync } from "fs";
import { join } from "path";
import { z } from "zod";
import { stringify as stringifyYaml } from "yaml";
import { MemoryLayer } from "../types/memory.js";
import {
  resolveProjectRoot,
  resolveGlobalRoot,
  isProjectInitialized,
  isGlobalInitialized,
} from "../storage/scope-resolver.js";
import { getDefaultConfig } from "../storage/config-loader.js";
import { validateInput } from "../types/validation.js";

const InitializeMemorySchema = z.object({
  scope: z.enum(["project", "global"]).optional(),
});

export const initializeMemoryTool = {
  name: "initialize_memory",
  description:
    "Initialize MemoryKit memory storage for this project. Run once before using other memory tools if they return initialization errors.",
  inputSchema: {
    type: "object",
    properties: {
      scope: {
        type: "string",
        enum: ["project", "global"],
        default: "project",
        description: "project (default) or global",
      },
    },
  },
};

export async function handleInitializeMemory(args: unknown): Promise<any> {
  const v = validateInput(InitializeMemorySchema, args);
  if (!v.success) {
    return {
      content: [{ type: "text", text: `Validation error: ${v.error}` }],
      isError: true,
    };
  }

  const isGlobal = v.data.scope === "global";
  const root = isGlobal ? resolveGlobalRoot() : resolveProjectRoot();
  const alreadyFullyInitialized = isGlobal
    ? isGlobalInitialized()
    : isProjectInitialized();

  // Always ensure directory structure exists (idempotent)
  mkdirSync(join(root, MemoryLayer.Working), { recursive: true });
  mkdirSync(join(root, MemoryLayer.Facts), { recursive: true });
  mkdirSync(join(root, MemoryLayer.Episodes), { recursive: true });
  mkdirSync(join(root, MemoryLayer.Procedures), { recursive: true });

  // Config file - create if missing
  const configPath = join(root, "memorykit.yaml");
  if (!existsSync(configPath)) {
    writeFileSync(configPath, stringifyYaml(getDefaultConfig()), "utf-8");
  }

  // Session template - create if missing
  const sessionPath = join(root, MemoryLayer.Working, "session.md");
  if (!existsSync(sessionPath)) {
    writeFileSync(
      sessionPath,
      "# Working Memory\n\nCurrent session context and active tasks.\n\n---\n",
      "utf-8",
    );
  }

  // Layer placeholders - create if missing
  for (const layer of [
    MemoryLayer.Facts,
    MemoryLayer.Episodes,
    MemoryLayer.Procedures,
  ]) {
    const gitkeep = join(root, layer, ".gitkeep");
    if (!existsSync(gitkeep)) {
      writeFileSync(gitkeep, "", "utf-8");
    }
  }

  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(
          {
            initialized: !alreadyFullyInitialized,
            already_existed: alreadyFullyInitialized,
            root,
            scope: v.data.scope ?? "project",
            message: alreadyFullyInitialized
              ? `Memory already initialized at ${root}`
              : `Memory initialized at ${root}`,
          },
          null,
          2,
        ),
      },
    ],
  };
}
