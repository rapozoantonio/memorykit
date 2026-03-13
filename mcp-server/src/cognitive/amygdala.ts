/**
 * Amygdala Importance Engine - Ported from C# AmygdalaImportanceEngine.cs
 * Calculates importance scores for memory entries using multiple signal components
 */

import type { ImportanceSignals } from "../types/cognitive.js";
import type { EntryContext } from "../types/memory.js";
import {
  DecisionPatternsWeighted,
  ImportanceMarkersWeighted,
  PositiveMarkers,
  NegativeMarkers,
  CodeKeywords,
  CommonWords,
} from "./patterns.js";

/**
 * Calculate importance score for content
 * Returns 0.05-0.95 (never absolute 0 or 1)
 */
export function calculateImportance(
  content: string,
  context?: EntryContext,
): number {
  const signals = calculateAllSignals(content, context);
  return computeGeometricMean(signals);
}

/**
 * Calculate all signal components
 */
function calculateAllSignals(
  content: string,
  context?: EntryContext,
): ImportanceSignals {
  return {
    decisionLanguage: detectDecisionLanguage(content),
    explicitImportance: detectExplicitImportance(content),
    question: detectQuestion(content),
    codeBlocks: detectCodeBlocks(content),
    novelty: detectNovelty(content, context),
    sentiment: detectSentiment(content),
    technicalDepth: detectTechnicalDepth(content),
    conversationContext: detectConversationContext(content),
    mmlStructure: detectMMLStructure(content),
  };
}

/**
 * Detect decision language patterns with weighted scoring
 */
function detectDecisionLanguage(content: string): number {
  const lower = content.toLowerCase();
  let score = 0;

  // Single pass through weighted patterns
  for (const [pattern, weight] of DecisionPatternsWeighted) {
    if (lower.includes(pattern)) {
      score = Math.max(score, weight);
    }
  }

  // Boost if combined with rationale
  if (score > 0 && /\b(because|due to|since|as|reason)\b/i.test(content)) {
    score = Math.min(score + 0.1, 1.0);
  }

  return Math.min(score, 1.0);
}

/**
 * Detect explicit importance markers with weighted scoring
 */
function detectExplicitImportance(content: string): number {
  const lower = content.toLowerCase();
  let score = 0;

  // Single pass through weighted markers
  for (const [marker, weight] of ImportanceMarkersWeighted) {
    if (lower.includes(marker)) {
      score = Math.max(score, weight);
    }
  }

  return Math.min(score, 1.0);
}

/**
 * Detect question patterns
 */
function detectQuestion(content: string): number {
  const trimmed = content.trimEnd();

  if (!trimmed.endsWith("?")) {
    return 0.05; // Slight boost for clarifying statements
  }

  // Decision-oriented questions are more important
  if (/\b(should|must|will|can|could|may)\s/i.test(content)) {
    return 0.4;
  }

  // Factual questions
  return 0.2;
}

/**
 * Detect code blocks and code references
 */
function detectCodeBlocks(content: string): number {
  // Fenced code blocks are highly important
  if (content.includes("```")) {
    return 0.6;
  }

  // Inline code
  if (content.includes("`") && /`[^`]+`/.test(content)) {
    return 0.45;
  }

  // Code-related keywords
  const lower = content.toLowerCase();
  for (const keyword of CodeKeywords) {
    if (lower.includes(keyword)) {
      return 0.3;
    }
  }

  return 0.0;
}

/**
 * Detect novelty (new concepts/entities)
 */
function detectNovelty(content: string, context?: EntryContext): number {
  if (!context?.existingEntries || context.existingEntries.length === 0) {
    return 0.3; // Moderate novelty default - balanced to avoid inflating trivial content while allowing factual content to score above floor
  }

  // Extract meaningful words
  const words = content
    .toLowerCase()
    .split(/\s+/)
    .filter((w) => w.length > 3 && !CommonWords.has(w));

  // Check against existing tags
  const existingTags = new Set(
    context.recentTags ??
      context.existingEntries
        .flatMap((e) => e.tags)
        .map((t) => t.toLowerCase()),
  );

  // Count novel words
  const novelWords = words.filter((w) => !existingTags.has(w));
  const noveltyRatio = words.length > 0 ? novelWords.length / words.length : 0;

  // Cap at 0.7
  return Math.min(noveltyRatio * 0.7, 0.7);
}

/**
 * Detect sentiment/emotional markers
 */
function detectSentiment(content: string): number {
  const lower = content.toLowerCase();

  let positiveCount = 0;
  let negativeCount = 0;

  for (const marker of PositiveMarkers) {
    if (lower.includes(marker)) {
      positiveCount++;
    }
  }

  for (const marker of NegativeMarkers) {
    if (lower.includes(marker)) {
      negativeCount++;
    }
  }

  const total = positiveCount + negativeCount;
  if (total === 0) return 0.0;

  // Problems/issues are worth remembering
  if (negativeCount > 0) {
    return Math.min(negativeCount * 0.15, 0.5);
  }

  // Positive intensity
  if (positiveCount > 0) {
    return Math.min(positiveCount * 0.1, 0.4);
  }

  return 0.0;
}

/**
 * Detect technical depth
 */
function detectTechnicalDepth(content: string): number {
  let score = 0.0;

  // Long content tends to be more detailed
  if (content.length > 500) {
    score += 0.2;
  }

  // Technical terms (uppercase acronyms, technical patterns)
  const acronymCount = (content.match(/\b[A-Z]{2,}\b/g) || []).length;
  if (acronymCount > 2) {
    score += 0.2;
  }

  // Code references
  if (content.includes("`") || content.includes("```")) {
    score += 0.2;
  }

  // Technical complexity indicators
  const complexityMarkers = [
    "algorithm",
    "implementation",
    "architecture",
    "design pattern",
    "optimization",
  ];
  for (const marker of complexityMarkers) {
    if (content.toLowerCase().includes(marker)) {
      score += 0.1;
      break;
    }
  }

  return Math.min(score, 1.0);
}

/**
 * Detect conversation context markers
 */
function detectConversationContext(content: string): number {
  const lower = content.toLowerCase();

  const contextMarkers = [
    "from now on",
    "going forward",
    "as we discussed",
    "remember that",
    "keep in mind",
    "for future reference",
  ];

  for (const marker of contextMarkers) {
    if (lower.includes(marker)) {
      return 0.6; // High importance for meta-decisions
    }
  }

  return 0.0;
}

/**
 * Detect MML structure (pre-normalized entries)
 * Returns higher scores when content contains structured MML keys
 * This avoids double-counting with prose-based signals
 */
function detectMMLStructure(content: string): number {
  // Check if content is in MML format (has ### heading and - **key**: value lines)
  if (!content.includes("- **")) {
    return 0.0; // Not MML format
  }

  // Extract MML keys present
  const mmlKeys = new Set<string>();
  const keyPattern = /- \*\*([a-z-]+)\*\*:/gi;
  let match;

  while ((match = keyPattern.exec(content)) !== null) {
    mmlKeys.add(match[1].toLowerCase());
  }

  if (mmlKeys.size === 0) {
    return 0.0; // No MML keys found
  }

  // Score based on key combinations
  // Strong decision: has rejected or constraint
  if (mmlKeys.has("rejected") || mmlKeys.has("constraint")) {
    return 0.7;
  }

  // Complete problem-resolution: has symptom + fix
  if (mmlKeys.has("symptom") && mmlKeys.has("fix")) {
    return 0.6;
  }

  // Complete procedure: has do + dont
  if (mmlKeys.has("do") && mmlKeys.has("dont")) {
    return 0.5;
  }

  // Partial procedure: has do only
  if (mmlKeys.has("do")) {
    return 0.3;
  }

  // Problem-related keys
  if (
    mmlKeys.has("symptom") ||
    mmlKeys.has("root-cause") ||
    mmlKeys.has("workaround")
  ) {
    return 0.4;
  }

  // Has why (rationale provided)
  if (mmlKeys.has("why")) {
    return 0.2;
  }

  // Basic MML structure detected but no special keys
  return 0.1;
}

/**
 * Compute geometric mean of signal scores
 * More robust than arithmetic mean - prevents single high signal from inflating score
 * Special handling for MML structure to avoid over-dampening
 */
function computeGeometricMean(signals: ImportanceSignals): number {
  // Check if MML structure is present (strong structural indicator)
  const hasMMLStructure = signals.mmlStructure > 0.1;

  // Filter out trivial signals
  const values = Object.values(signals).filter((s) => s > 0.01);

  if (values.length === 0) {
    return 0.1; // Minimum floor
  }

  // For MML content, use weighted geometric mean to preserve MML signal strength
  if (hasMMLStructure) {
    // Separate MML signal from others
    const mmlScore = signals.mmlStructure;
    const otherSignals = values.filter((v) => v !== mmlScore);

    if (otherSignals.length === 0) {
      // Only MML signal present
      return Math.max(0.1, Math.min(0.95, mmlScore * 0.9));
    }

    // Weighted combination: 70% MML signal, 30% geometric mean of others
    const otherProduct = otherSignals.reduce((acc, val) => acc * val, 1);
    const otherGeomean = Math.pow(otherProduct, 1.0 / otherSignals.length);

    const combined = mmlScore * 0.7 + otherGeomean * 0.3;

    // Apply dampening and clamp
    return Math.max(0.1, Math.min(0.95, combined * 0.9));
  }

  // For prose content, use max-weighted approach (mirrors MML pattern)
  // Prevents "more signals = lower score" problem with pure geometric mean
  const maxSignal = Math.max(...values);

  if (values.length === 1) {
    return Math.max(0.1, Math.min(0.95, maxSignal * 0.9));
  }

  const otherSignals = values.filter((v) => v !== maxSignal);
  const otherProduct = otherSignals.reduce((acc, val) => acc * val, 1);
  const otherGeomean = Math.pow(otherProduct, 1.0 / otherSignals.length);

  // 30% max signal, 70% geometric mean of others
  // More conservative than MML's 70-30 to avoid over-scoring prose
  const combined = maxSignal * 0.3 + otherGeomean * 0.7;
  const dampened = combined * 0.9;

  return Math.max(0.1, Math.min(0.95, dampened));
}

/**
 * Calculate recency factor for importance decay
 */
export function calculateRecencyFactor(
  createdDate: string,
  decayRate: number = 1.0,
): number {
  const created = new Date(createdDate);
  const now = new Date();
  const daysSince = (now.getTime() - created.getTime()) / (1000 * 60 * 60 * 24);

  // Exponential decay
  const factor = Math.exp((-daysSince * decayRate) / 7.0); // 7-day half-life

  return Math.max(0.1, factor); // Never less than 0.1
}

/**
 * Calculate effective score (importance × recency)
 */
export function calculateEffectiveScore(
  importance: number,
  createdDate: string,
  decayRate?: number,
): number {
  const recencyFactor = calculateRecencyFactor(createdDate, decayRate);
  return importance * recencyFactor;
}
