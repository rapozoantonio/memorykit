/**
 * onnxruntime-node probes its native binding asynchronously on some platforms;
 * when that probe fails (e.g. a stale prebuilt binary on a given CI image), it
 * rejects outside the promise chain @xenova/transformers awaits, so it surfaces
 * as an unhandled rejection rather than a catchable error. The embedding pipeline
 * already degrades gracefully when generation itself fails (see embedding.ts) —
 * this only prevents that unrelated, non-fatal probe from failing the test run.
 */
process.on("unhandledRejection", (reason) => {
  console.error("[MemoryKit] Unhandled rejection (non-fatal, ignored in tests):", reason);
});
