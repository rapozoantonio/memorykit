import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    globals: false,
    environment: "node",
    include: ["src/**/*.test.ts"],
    testTimeout: 15000, // E2E tests with temp-dir I/O need more time
    coverage: {
      reporter: ["text", "json"],
      include: ["src/**/*.ts"],
      exclude: [
        "src/**/*.test.ts",
        "src/__tests__/**",
        "src/cognitive/__tests__/**",
        // Dead code stubs
        "src/api-client.ts",
        "src/process-manager.ts",
        "src/process-manager-dev.ts",
        "src/index-dev.ts",
        "src/tools/index.ts",
      ],
    },
  },
});
