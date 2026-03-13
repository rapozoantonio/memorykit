/**
 * Cognitive patterns - Regex patterns and constants ported from C# AmygdalaImportanceEngine and PrefrontalController
 */

// Decision language patterns
export const DecisionPatterns = [
  "i will",
  "let's",
  "we should",
  "i decided",
  "going to",
  "plan to",
  "commit to",
  "i'll",
  "we'll",
  "must",
];

// Importance marker patterns
export const ImportanceMarkers = [
  "important",
  "critical",
  "remember",
  "don't forget",
  "note that",
  "always",
  "never",
  "from now on",
  "crucial",
  "essential",
  "key point",
  "take note",
];

// Positive sentiment markers
export const PositiveMarkers = [
  "great",
  "excellent",
  "perfect",
  "amazing",
  "wonderful",
  "fantastic",
  "awesome",
  "love",
  "thank you",
  "thanks",
];

// Negative sentiment markers
export const NegativeMarkers = [
  "problem",
  "issue",
  "error",
  "bug",
  "fail",
  "wrong",
  "broken",
  "crash",
  "urgent",
  "critical",
  "emergency",
];

// Code-related keywords
export const CodeKeywords = [
  "function",
  "class",
  "method",
  "algorithm",
  "implementation",
];

// Weighted decision patterns for scoring
export const DecisionPatternsWeighted: [string, number][] = [
  ["decided", 0.5],
  ["decided to", 0.5],
  ["committed", 0.5],
  ["will commit", 0.5],
  ["final decision", 0.5],
  ["i choose", 0.5],
  [" will ", 0.25],
  ["going to", 0.25],
  ["plan to", 0.25],
  ["consider", 0.15],
  ["thinking about", 0.15],
  ["maybe", 0.15],
  ["might", 0.15],
  ["considering", 0.15],
];

// Weighted importance markers for scoring
export const ImportanceMarkersWeighted: [string, number][] = [
  ["critical", 0.6],
  ["crucial", 0.6],
  ["essential", 0.6],
  ["must", 0.6],
  ["required", 0.6],
  ["vital", 0.6],
  ["important", 0.4],
  ["remember", 0.4],
  ["note that", 0.4],
  ["key point", 0.4],
  ["significant", 0.4],
  ["don't forget", 0.35],
  ["important to note", 0.35],
  ["remember to", 0.35],
  ["take note", 0.35],
  ["pay attention", 0.35],
];

// Continuation patterns (for Prefrontal)
export const ContinuationPatterns = [
  "continue",
  "go on",
  "and then",
  "next",
  "keep going",
  "more",
];

// Fact retrieval phrases (for Prefrontal)
export const FactRetrievalPhrases = [
  "what was",
  "what is",
  "who is",
  "when did",
  "how many",
  "tell me about",
  "remind me",
];

// Fact retrieval tokens (for Prefrontal)
export const FactRetrievalTokens = new Set([
  "what",
  "where",
  "when",
  "who",
  "which",
  "why",
  "how",
]);

// Deep recall patterns (for Prefrontal)
export const DeepRecallPatterns = [
  "quote",
  "exactly",
  "verbatim",
  "word for word",
  "precise",
  "show me the",
  "find the conversation",
];

// Procedural trigger tokens (for Prefrontal)
export const ProceduralTriggerTokens = new Set([
  "create",
  "generate",
  "build",
  "implement",
  "format",
  "structure",
  "write",
]);

// Question words (for Prefrontal)
export const QuestionWords = new Set([
  "what",
  "where",
  "when",
  "who",
  "which",
  "why",
]);

// Retrieval phrases (for Prefrontal)
export const RetrievalPhrases = [
  "find",
  "show",
  "get",
  "tell me",
  "retrieve",
  "look up",
  "search",
  "remind me",
];

// Decision modals (for Prefrontal)
export const DecisionModals = [
  "should",
  "shall",
  "ought",
  "must",
  "can we",
  "could we",
];

// Decision verbs (for Prefrontal)
export const DecisionVerbs = new Set([
  "decide",
  "choose",
  "commit",
  "select",
  "pick",
  "adopt",
  "implement",
]);

// Common words to exclude from novelty/relevance detection
export const CommonWords = new Set([
  "the",
  "and",
  "for",
  "with",
  "this",
  "that",
  "from",
  "have",
  "been",
  "will",
  "would",
  "could",
  "should",
  "about",
  "which",
  "their",
  "there",
  // Additional for relevance scoring
  "a",
  "an",
  "is",
  "are",
  "was",
  "were",
  "be",
  "has",
  "had",
  "do",
  "does",
  "did",
  "may",
  "might",
  "can",
  "what",
  "how",
  "when",
  "where",
  "who",
  "we",
  "i",
  "you",
  "it",
  "to",
  "of",
  "in",
  "on",
  "at",
  "use",
  "using",
]);
