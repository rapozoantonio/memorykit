/**
 * Write-Time Quality Gates
 * Prevents bad memory before it's stored
 */

import type { MemoryEntry } from "../types/memory.js";

export interface GateResult {
  pass: boolean;
  reason?: string;
  suggestion?: string;
}

/**
 * Gate 1: Importance Floor
 * Reject entries below configured threshold
 */
export function checkImportanceFloor(
  importance: number,
  threshold: number = 0.15,
): GateResult {
  if (importance < threshold) {
    return {
      pass: false,
      reason: `Content scored ${importance.toFixed(2)}, below threshold ${threshold}`,
      suggestion: "Content appears routine. Not stored.",
    };
  }
  return { pass: true };
}

/**
 * Gate 2: Near-Duplicate Detection
 * Check for similar entries based on tags and content
 */
export function checkDuplicate(
  newEntry: {
    what: string;
    tags: string[];
  },
  existingEntries: MemoryEntry[],
): GateResult {
  for (const existing of existingEntries) {
    // Calculate Jaccard similarity for tags
    const tagOverlap = jaccardSimilarity(newEntry.tags, existing.tags);

    if (tagOverlap >= 0.6) {
      // Check content overlap (significant words)
      const contentOverlap = significantWordOverlap(
        newEntry.what,
        existing.what,
      );

      if (contentOverlap >= 3) {
        return {
          pass: false,
          reason: `Near-duplicate of existing memory: "${existing.title}"`,
          suggestion: `Use update_memory with entry_id "${existing.id}" to modify, or adjust tags if this is distinct knowledge.`,
        };
      }
    }
  }

  return { pass: true };
}

/**
 * Gate 3: Contradiction Detection
 * Warns if new entry contradicts existing knowledge (doesn't block)
 */
export function checkContradiction(
  newEntry: {
    what: string;
    tags: string[];
  },
  existingEntries: MemoryEntry[],
): GateResult {
  for (const existing of existingEntries) {
    // Only check entries with related tags
    const tagOverlap = jaccardSimilarity(newEntry.tags, existing.tags);
    if (tagOverlap < 0.4) continue;

    // Extract primary entities
    const newEntity = extractPrimaryEntity(newEntry.what);
    const existingEntity = extractPrimaryEntity(existing.what);

    // Check if same entity with different information
    if (
      newEntity &&
      existingEntity &&
      newEntity.toLowerCase() === existingEntity.toLowerCase() &&
      newEntry.what !== existing.what
    ) {
      return {
        pass: true, // Still allow storage
        reason: `Potential conflict with existing memory: "${existing.title}"`,
        suggestion: `Consider updating entry "${existing.id}" instead of creating a new one. The existing entry may be stale.`,
      };
    }
  }

  return { pass: true };
}

/**
 * Calculate Jaccard similarity between two tag sets
 * Returns value between 0 and 1
 */
function jaccardSimilarity(tags1: string[], tags2: string[]): number {
  if (tags1.length === 0 && tags2.length === 0) return 1;
  if (tags1.length === 0 || tags2.length === 0) return 0;

  const set1 = new Set(tags1.map((t) => t.toLowerCase()));
  const set2 = new Set(tags2.map((t) => t.toLowerCase()));

  const intersection = new Set([...set1].filter((x) => set2.has(x)));
  const union = new Set([...set1, ...set2]);

  return intersection.size / union.size;
}

/**
 * Count overlap of significant words between two texts
 * Excludes common stop words
 */
function significantWordOverlap(text1: string, text2: string): number {
  const stopWords = new Set([
    "a",
    "an",
    "and",
    "are",
    "as",
    "at",
    "be",
    "by",
    "for",
    "from",
    "has",
    "have",
    "in",
    "is",
    "it",
    "its",
    "of",
    "on",
    "that",
    "the",
    "to",
    "was",
    "were",
    "will",
    "with",
  ]);

  // Extract significant words (nouns, verbs, adjectives)
  const words1 = text1
    .toLowerCase()
    .split(/\W+/)
    .filter((w) => w.length > 3 && !stopWords.has(w));

  const words2 = text2
    .toLowerCase()
    .split(/\W+/)
    .filter((w) => w.length > 3 && !stopWords.has(w));

  const set1 = new Set(words1);
  const set2 = new Set(words2);

  // Count intersection
  let overlap = 0;
  for (const word of set1) {
    if (set2.has(word)) {
      overlap++;
    }
  }

  return overlap;
}

/**
 * Extract primary entity from text (the subject being described)
 * Handles various patterns found in normalized MML content
 */
function extractPrimaryEntity(text: string): string | null {
  // Pattern 1: "primary/main/core [noun]" - extract the qualified noun
  const qualifiedNounMatch = text.match(
    /\b(primary|main|core|default)\s+(\w+)/i,
  );
  if (qualifiedNounMatch) {
    return `${qualifiedNounMatch[1]} ${qualifiedNounMatch[2]}`.toLowerCase();
  }

  // Pattern 2: "[subject] is/are/uses [value]" - extract subject
  const subjectMatch = text.match(/([\w\s]+?)(?:\s+(?:is|are|uses|has)\s+)/i);
  if (subjectMatch) {
    const subject = subjectMatch[1].trim();
    // Only return if it's a substantial phrase (2+ words or 1 significant word)
    if (subject.split(/\s+/).length >= 2 || subject.length >= 6) {
      return subject.toLowerCase();
    }
  }

  // Pattern 3: "migrating/moving/switching [noun]" - extract object
  const actionObjectMatch = text.match(
    /(?:migrating|moving|switching|changing)\s+([\w\s]+?)(?:\s+to\s+)/i,
  );
  if (actionObjectMatch) {
    return actionObjectMatch[1].trim().toLowerCase();
  }

  // Pattern 4: "use/using [name] for [purpose]" - extract name
  const useForMatch = text.match(/(?:use|using)\s+([\w\s]+?)\s+for\s+/i);
  if (useForMatch) {
    return useForMatch[1].trim().toLowerCase();
  }

  // Pattern 5: Technology names (capitalized with optional version)
  const techMatch = text.match(/\b([A-Z][a-zA-Z0-9]*(?:\s+[0-9.]+)?)\b/);
  if (techMatch) {
    return techMatch[1].toLowerCase();
  }

  // Pattern 6: First significant noun phrase (2+ words or 6+ chars)
  const words = text.split(/\s+/);
  if (words.length >= 2) {
    const firstTwo = words.slice(0, 2).join(" ");
    const cleaned = firstTwo.replace(/[^a-zA-Z0-9\s]/g, "");
    if (cleaned.length >= 6) {
      return cleaned.toLowerCase();
    }
  }

  return null;
}
