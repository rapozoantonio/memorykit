/**
 * Tier 1: Local Embedding Generation
 * Uses @xenova/transformers to generate semantic embeddings locally
 * No API calls, runs entirely on user's machine
 */

// Cache the pipeline instance
let embedder: any = null;

const MODEL_LOAD_TIMEOUT_MS = 30_000;

function withTimeout<T>(promise: Promise<T>, ms: number, message: string): Promise<T> {
  return Promise.race([
    promise,
    new Promise<T>((_, reject) =>
      setTimeout(() => reject(new Error(message)), ms),
    ),
  ]);
}

/**
 * Initialize the embedding pipeline (lazy loaded)
 * Uses all-MiniLM-L6-v2: 384 dimensions, ~23MB, optimized for semantic similarity
 *
 * The @xenova/transformers import is dynamic (not static) because it loads a
 * platform-specific native binary (onnxruntime-node). On unsupported platforms
 * (e.g. some ARM64 Linux setups) that load can fail — a static top-level import
 * would crash the whole server at startup; a dynamic import here lets callers
 * (store.ts, retrieve.ts) catch the failure and degrade to keyword-only search.
 */
async function getEmbedder() {
  if (process.env.MEMORYKIT_SKIP_EMBEDDINGS === "true") {
    throw new Error("Embeddings disabled via MEMORYKIT_SKIP_EMBEDDINGS");
  }

  if (!embedder) {
    console.error("[MemoryKit] Loading embedding model (first run, ~23MB)...");
    const { pipeline, env } = await import("@xenova/transformers");
    env.allowLocalModels = true;

    embedder = await withTimeout(
      pipeline("feature-extraction", "Xenova/all-MiniLM-L6-v2", {
        quantized: true, // Use quantized version for smaller size
      }),
      MODEL_LOAD_TIMEOUT_MS,
      "Embedding model load timed out — falling back to keyword search",
    );
  }
  return embedder;
}

/**
 * Generate embedding vector for text
 * Returns 384-dimensional vector normalized to unit length
 */
export async function embedText(text: string): Promise<number[]> {
  const pipe = await getEmbedder();
  const output = await pipe(text, {
    pooling: "mean",
    normalize: true,
  });
  return Array.from(output.data);
}

/**
 * Calculate cosine similarity between two embedding vectors
 * Returns value between -1 (opposite) and 1 (identical)
 * For normalized vectors: dot product = cosine similarity
 */
export function cosineSimilarity(a: number[], b: number[]): number {
  if (a.length !== b.length) {
    throw new Error(`Vector dimension mismatch: ${a.length} vs ${b.length}`);
  }

  let dotProduct = 0;
  for (let i = 0; i < a.length; i++) {
    dotProduct += a[i] * b[i];
  }

  return dotProduct; // Already normalized during embedding
}

/**
 * Batch embed multiple texts (more efficient than one-by-one)
 */
export async function embedBatch(texts: string[]): Promise<number[][]> {
  if (texts.length === 0) return [];

  const pipe = await getEmbedder();
  const results: number[][] = [];

  // Process in batches of 32 for memory efficiency
  const batchSize = 32;
  for (let i = 0; i < texts.length; i += batchSize) {
    const batch = texts.slice(i, i + batchSize);
    for (const text of batch) {
      const output = await pipe(text, {
        pooling: "mean",
        normalize: true,
      });
      results.push(Array.from(output.data));
    }
  }

  return results;
}
