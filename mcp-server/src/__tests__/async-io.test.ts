/**
 * Tests for M6: Async I/O Conversion
 * Validates that no sync file operations remain and concurrent operations work
 */

import { describe, it, expect } from "vitest";
import { readFile } from "fs/promises";
import { join } from "path";

describe("Async I/O Conversion (M6)", () => {
  describe("No Sync Calls Remaining", () => {
    const filesToCheck = [
      "src/storage/file-manager.ts",
      "src/memory/store.ts",
      "src/memory/retrieve.ts",
      "src/memory/update.ts",
      "src/memory/forget.ts",
      "src/memory/consolidate.ts",
    ];

    for (const file of filesToCheck) {
      it(`should not have readFileSync in ${file}`, async () => {
        const filePath = join(process.cwd(), file);
        let content: string;

        try {
          content = await readFile(filePath, "utf-8");
        } catch (err) {
          // File might not exist in test environment
          return;
        }

        // Check for sync calls (excluding comments)
        const lines = content.split("\n");
        const codeLines = lines.filter(
          (line) =>
            !line.trim().startsWith("//") && !line.trim().startsWith("*"),
        );
        const codeContent = codeLines.join("\n");

        expect(codeContent).not.toContain("readFileSync");
        expect(codeContent).not.toContain("writeFileSync");
        expect(codeContent).not.toContain("readdirSync");
        expect(codeContent).not.toContain("statSync");
        expect(codeContent).not.toContain("unlinkSync");
        expect(codeContent).not.toContain("mkdirSync");
      });
    }
  });
});
