/**
 * Cognitive layer type definitions - Amygdala and Prefrontal systems
 */

/**
 * Query types for classification
 */
export enum QueryType {
  Continuation = "continuation",
  FactRetrieval = "factRetrieval",
  DeepRecall = "deepRecall",
  ProceduralTrigger = "procedural",
  Complex = "complex",
  Store = "store",
}

/**
 * Signal components for importance scoring
 */
export interface ImportanceSignals {
  decisionLanguage: number;
  explicitImportance: number;
  question: number;
  codeBlocks: number;
  novelty: number;
  sentiment: number;
  technicalDepth: number;
  conversationContext: number;
  mmlStructure: number;
}

/**
 * Query classification result
 */
export interface QueryClassification {
  type: QueryType;
  confidence: number;
}

/**
 * File set for retrieval (project + global scope)
 */
export interface FileSet {
  project: string[];
  global: string[];
}

/**
 * Query signals for classification
 */
export interface QuerySignals {
  retrievalSignal: number;
  decisionSignal: number;
  patternSignal: number;
  narrativeSignal: number;
}
