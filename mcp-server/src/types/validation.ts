/**
 * Zod input validation schemas for all MCP tool handlers
 */

import { z } from "zod";

// ─── Shared enums ──────────────────────────────────────────────────────────────

export const MemoryLayerEnum = z.enum([
  "working",
  "facts",
  "episodes",
  "procedures",
]);

export const MemoryScopeEnum = z.enum(["project", "global"]);

export const RetrieveScopeEnum = z.enum(["all", "project", "global"]);

export const ConsolidateScopeEnum = z.enum(["project", "global", "all"]);

// ─── Tool schemas ──────────────────────────────────────────────────────────────

export const StoreMemorySchema = z.object({
  content: z.string().min(1, "content must not be empty"),
  tags: z.array(z.string()).optional(),
  layer: MemoryLayerEnum.optional(),
  scope: MemoryScopeEnum.optional(),
  file_hint: z.string().optional(),
  acquisition_context: z
    .object({
      tokens_consumed: z.number().int().nonnegative(),
      tool_calls: z.number().int().nonnegative(),
    })
    .optional(),
});

export const RetrieveContextSchema = z.object({
  query: z.string().min(1, "query must not be empty"),
  max_tokens: z.number().int().positive().optional(),
  layers: z.array(MemoryLayerEnum).optional(),
  scope: RetrieveScopeEnum.optional(),
});

export const UpdateMemorySchema = z.object({
  entry_id: z.string().min(1, "entry_id must not be empty"),
  content: z.string().optional(),
  tags: z.array(z.string()).optional(),
  importance: z.number().min(0).max(1).optional(),
});

export const ForgetMemorySchema = z.object({
  entry_id: z.string().min(1, "entry_id must not be empty"),
});

export const ConsolidateSchema = z.object({
  scope: ConsolidateScopeEnum.optional(),
  dry_run: z.boolean().optional(),
});

export const ListMemoriesSchema = z.object({
  scope: RetrieveScopeEnum.optional(),
  layer: MemoryLayerEnum.optional(),
});

// ─── Validation helper ─────────────────────────────────────────────────────────

type ValidationSuccess<T> = { success: true; data: T };
type ValidationFailure = { success: false; error: string };

/**
 * Validate args against a Zod schema and return a discriminated union result.
 * On failure, error is a human-readable string suitable for MCP error response.
 */
export function validateInput<T>(
  schema: z.ZodSchema<T>,
  args: unknown,
): ValidationSuccess<T> | ValidationFailure {
  const result = schema.safeParse(args);
  if (result.success) {
    return { success: true, data: result.data };
  }
  const message = result.error.errors
    .map((e) =>
      e.path.length > 0 ? `${e.path.join(".")}: ${e.message}` : e.message,
    )
    .join("; ");
  return { success: false, error: message };
}
