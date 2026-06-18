import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    globals: false,
    environment: "node",
    include: ["src/**/*.test.ts"],
    setupFiles: ["src/__tests__/setup.ts"],
    testTimeout: 15000, // E2E tests with temp-dir I/O need more time
    coverage: {
      reporter: ["text", "json"],
      include: ["src/**/*.ts"],
      exclude: [
        "src/**/*.test.ts",
        "src/__tests__/**",
        "src/cognitive/__tests__/**",
      ],
    },
  },
});
