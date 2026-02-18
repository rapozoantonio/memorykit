/**
 * Prefrontal Controller - Ported from C# PrefrontalController.cs
 * Query classification and intelligent routing to memory layers
 */

import type {
  QueryClassification,
  QueryType,
  FileSet,
} from "../types/cognitive.js";
import type { MemoryLayer } from "../types/memory.js";
import type { MemoryKitConfig } from "../types/config.js";
import { MemoryLayer as Layer } from "../types/memory.js";
import { QueryType as QType } from "../types/cognitive.js";
import {
  ContinuationPatterns,
  FactRetrievalPhrases,
  FactRetrievalTokens,
  DeepRecallPatterns,
  ProceduralTriggerTokens,
} from "./patterns.js";

/**
 * Classify query to determine retrieval strategy
 */
export function classifyQuery(query: string): QueryClassification {
  // Fast-path pattern matching (handles ~80% of queries)
  const quick = quickClassify(query);
  if (quick) {
    return quick;
  }

  // Fallback: complex query
  return {
    type: QType.Complex,
    confidence: 0.4,
  };
}

/**
 * Quick pattern-based classification for common query types
 */
function quickClassify(query: string): QueryClassification | null {
  const lower = query.toLowerCase().trim();

  // Continuation patterns (check prefix first - most specific)
  for (const pattern of ContinuationPatterns) {
    if (lower.startsWith(pattern)) {
      return {
        type: QType.Continuation,
        confidence: 0.9,
      };
    }
  }

  // Tokenize once for efficient matching
  const tokens = lower.split(/[\s,.\!?;:\t\n]+/).filter((t) => t);
  const tokenSet = new Set(tokens);

  // Check multi-word phrases for fact retrieval
  for (const phrase of FactRetrievalPhrases) {
    if (lower.includes(phrase)) {
      return {
        type: QType.FactRetrieval,
        confidence: 0.8,
      };
    }
  }

  // Check single-word tokens against fact retrieval set
  if (hasOverlap(tokenSet, FactRetrievalTokens)) {
    return {
      type: QType.FactRetrieval,
      confidence: 0.75,
    };
  }

  // Deep recall patterns (exact phrase matching)
  for (const pattern of DeepRecallPatterns) {
    if (lower.includes(pattern)) {
      return {
        type: QType.DeepRecall,
        confidence: 0.75,
      };
    }
  }

  // Procedural trigger patterns (check for action tokens)
  if (hasOverlap(tokenSet, ProceduralTriggerTokens)) {
    return {
      type: QType.ProceduralTrigger,
      confidence: 0.7,
    };
  }

  // Signal-based classification for ambiguous queries
  const signals = {
    hasQuestionMark: query.includes("?"),
    wordCount: tokens.length,
    hasTimeReference: /\b(yesterday|last week|before|ago|when|history)\b/i.test(
      query,
    ),
    hasHowTo: /\b(how|steps|process|guide)\b/i.test(query),
    hasTechTerms: /\b(api|database|deploy|config|auth|test)\b/i.test(query),
    isShort: tokens.length < 5,
  };

  if (signals.isShort && !signals.hasQuestionMark) {
    return {
      type: QType.Continuation,
      confidence: 0.6,
    };
  }

  if (signals.hasTimeReference) {
    return {
      type: QType.DeepRecall,
      confidence: 0.6,
    };
  }

  if (signals.hasHowTo) {
    return {
      type: QType.ProceduralTrigger,
      confidence: 0.6,
    };
  }

  if (signals.hasTechTerms && signals.hasQuestionMark) {
    return {
      type: QType.FactRetrieval,
      confidence: 0.6,
    };
  }

  return null; // No quick classification
}

/**
 * Check if two sets have any overlap
 */
function hasOverlap<T>(set1: Set<T>, set2: Set<T>): boolean {
  for (const item of set1) {
    if (set2.has(item)) {
      return true;
    }
  }
  return false;
}

/**
 * Resolve which files to read based on query classification
 */
export function resolveFiles(
  classification: QueryClassification,
  config: MemoryKitConfig,
): FileSet {
  switch (classification.type) {
    case QType.Continuation:
      return {
        project: ["working/session.md"],
        global: [],
      };

    case QType.FactRetrieval:
      return {
        project: ["facts/*.md", "working/session.md"],
        global: ["facts/*.md"],
      };

    case QType.DeepRecall:
      return {
        project: ["episodes/*.md", "facts/*.md"],
        global: [],
      };

    case QType.ProceduralTrigger:
      return {
        project: ["procedures/*.md", "working/session.md"],
        global: ["procedures/*.md"],
      };

    case QType.Complex:
      return {
        project: ["facts/*.md", "working/session.md", "procedures/*.md"],
        global: ["facts/*.md", "procedures/*.md"],
      };

    default:
      return {
        project: ["working/session.md"],
        global: [],
      };
  }
}

/**
 * Determine memory layers to use for a query type
 */
export function determineLayersToUse(queryType: QueryType): MemoryLayer[] {
  switch (queryType) {
    case QType.Continuation:
      return [Layer.Working];

    case QType.FactRetrieval:
      return [Layer.Working, Layer.Facts];

    case QType.DeepRecall:
      return [Layer.Working, Layer.Facts, Layer.Episodes];

    case QType.ProceduralTrigger:
      return [Layer.Working, Layer.Procedures];

    case QType.Complex:
      return [Layer.Working, Layer.Facts, Layer.Episodes, Layer.Procedures];

    default:
      return [Layer.Working];
  }
}

/**
 * Estimate token budget for query type
 */
export function estimateTokenBudget(queryType: QueryType): number {
  switch (queryType) {
    case QType.Continuation:
      return 200;
    case QType.FactRetrieval:
      return 500;
    case QType.DeepRecall:
      return 1500;
    case QType.ProceduralTrigger:
      return 300;
    case QType.Complex:
      return 2000;
    default:
      return 500;
  }
}
