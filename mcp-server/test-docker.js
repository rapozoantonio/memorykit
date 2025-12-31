// Quick test script for Docker-based MCP server
import { ProcessManager } from "./dist/process-manager.js";

console.log("Testing Docker-based MCP server...\n");

const manager = new ProcessManager({
  useDocker: true,
  port: 5555,
  apiKey: "mcp-local-key",
});

try {
  console.log("Starting API...");
  await manager.start();

  console.log("\n✓ API started successfully!");
  console.log(`Base URL: ${manager.getBaseUrl()}`);
  console.log(`API Key: ${manager.getApiKey()}`);

  console.log("\nTesting health endpoint...");
  const response = await fetch(`${manager.getBaseUrl()}/health`);
  const health = await response.text(); // Health endpoint returns plain text
  console.log("Health response:", health);

  if (response.status !== 200) {
    throw new Error(`Health check failed with status ${response.status}`);
  }

  console.log("\n✓ All tests passed!");
  console.log("✓ Docker process manager working correctly!");
  console.log("\nStopping API...");
  await manager.stop();
  console.log("✓ Stopped");
  process.exit(0);
} catch (error) {
  console.error("\n✗ Test failed:", error.message);
  await manager.stop();
  process.exit(1);
}
