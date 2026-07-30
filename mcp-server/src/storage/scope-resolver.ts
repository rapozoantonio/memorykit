/**
 * Scope resolver - Resolve paths for project and global memory
 */

import { existsSync, renameSync } from "fs";
import { join, resolve, dirname, basename } from "path";
import { homedir } from "os";
import { createHash } from "crypto";
import type { MemoryLayer } from "../types/memory.js";
import { MemoryScope } from "../types/memory.js";

/**
 * Find git repository root by searching for .git directory
 */
function findGitRoot(startPath: string): string | null {
  let current = resolve(startPath);
  while (true) {
    if (existsSync(join(current, ".git"))) {
      return current;
    }
    const parent = dirname(current);
    if (parent === current) {
      return null;
    }
    current = parent;
  }
}

/**
 * Find project root by searching for common project markers
 */
function findProjectMarker(startPath: string): string | null {
  const markers = [
    "package.json",
    ".sln",
    "go.mod",
    "Cargo.toml",
    "pom.xml",
    "pyproject.toml",
    "composer.json",
  ];
  let current = resolve(startPath);
  while (true) {
    if (markers.some((m) => existsSync(join(current, m)))) {
      return current;
    }
    const parent = dirname(current);
    if (parent === current) {
      return null;
    }
    current = parent;
  }
}

/**
 * Get working directory, auto-detecting project root
 * Priority: MEMORYKIT_PROJECT env var → git root → project markers → cwd
 */
export function getWorkingDirectory(): string {
  // 1. Explicit override (from VS Code ${workspaceFolder})
  const envProject = process.env.MEMORYKIT_PROJECT;
  if (envProject && envProject.trim() !== "") {
    return envProject;
  }

  // 2. Fallback to cwd-based detection
  const startPath = process.cwd();

  // 3. Try to find git root
  const gitRoot = findGitRoot(startPath);
  if (gitRoot) {
    return gitRoot;
  }

  // 4. Try to find project marker
  const projectRoot = findProjectMarker(startPath);
  if (projectRoot) {
    return projectRoot;
  }

  // 5. Fallback to cwd
  return startPath;
}

// resolveProjectRoot() is called on nearly every tool invocation (retrieve,
// store, forget, update, consolidate, entity-graph, ...). The legacy-folder
// migration check below does synchronous disk I/O, so it's gated to run at
// most once per resolved root per process instead of on every call.
const _migrationChecked = new Set<string>();

/**
 * Resolve project root (~/.memorykit/<project-name>-<path-hash>/ directory)
 * Folder name is human-readable but suffixed with a hash of the absolute
 * project path, so two unrelated repos with the same folder name (e.g. two
 * "api" projects) don't silently share a memory namespace.
 */
export function resolveProjectRoot(): string {
  const projectPath = getWorkingDirectory();
  let projectName = basename(projectPath);

  // Safety: if project name is empty, a drive letter, or invalid, use "default"
  if (
    !projectName ||
    projectName.length <= 2 ||
    projectName === "." ||
    projectName === ".."
  ) {
    projectName = "default";
  }

  const pathHash = createHash("sha256")
    .update(resolve(projectPath))
    .digest("hex")
    .slice(0, 8);
  const dirName = `${projectName}-${pathHash}`;

  const memoryKitRoot = join(homedir(), ".memorykit");
  const newRoot = join(memoryKitRoot, dirName);

  // One-time migration: versions before the path hash keyed memory storage
  // by folder name alone, which collided for same-named repos on the same
  // machine. Move existing data into the hashed directory so upgrading
  // doesn't orphan a user's memory history.
  if (!_migrationChecked.has(newRoot)) {
    _migrationChecked.add(newRoot);
    const legacyRoot = join(memoryKitRoot, projectName);
    if (!existsSync(newRoot) && existsSync(legacyRoot)) {
      try {
        renameSync(legacyRoot, newRoot);
      } catch (error) {
        console.error(
          `Failed to migrate legacy memory directory ${legacyRoot} -> ${newRoot}:`,
          error,
        );
      }
    }
  }

  return newRoot;
}

/**
 * Resolve global root (~/.memorykit/ directory)
 */
export function resolveGlobalRoot(): string {
  return join(homedir(), ".memorykit");
}

/**
 * Resolve root based on scope
 */
export function resolveScopeRoot(scope: MemoryScope): string {
  return scope === MemoryScope.Project
    ? resolveProjectRoot()
    : resolveGlobalRoot();
}

/**
 * Resolve layer path within a scope
 */
export function resolveLayerPath(
  scope: MemoryScope,
  layer: MemoryLayer,
): string {
  const root = resolveScopeRoot(scope);
  return join(root, layer);
}

/**
 * Resolve file path within a scope and layer
 */
export function resolveFilePath(
  scope: MemoryScope,
  layer: MemoryLayer,
  filename: string,
): string {
  const layerPath = resolveLayerPath(scope, layer);

  // Prevent path traversal (defensive coding)
  const sanitized = filename.replace(/\\/g, "/");
  if (sanitized.includes("..") || sanitized.startsWith("/")) {
    throw new Error(`Invalid filename: ${filename}`);
  }

  // Ensure filename has .md extension
  const mdFilename = filename.endsWith(".md") ? filename : `${filename}.md`;

  return join(layerPath, mdFilename);
}

/**
 * Check if project memory is initialized
 */
export function isProjectInitialized(): boolean {
  const projectRoot = resolveProjectRoot();
  return existsSync(projectRoot);
}

/**
 * Check if global memory is initialized
 */
export function isGlobalInitialized(): boolean {
  const globalRoot = resolveGlobalRoot();
  return existsSync(globalRoot);
}

/**
 * Get config file path for a scope
 */
export function getConfigPath(scope: MemoryScope): string {
  const root = resolveScopeRoot(scope);
  return join(root, "memorykit.yaml");
}
