/**
 * Prose-to-MML Normalization Pipeline
 * Converts free-form prose to structured MML format
 */

import type { MemoryLayer } from "../types/memory.js";

export interface NormalizedEntry {
  title: string;
  fields: Record<string, string>;
  tags: string[];
  importance: number;
}

/**
 * Content type classification
 */
type ContentType = "decision" | "problem" | "rule" | "generic" | "already-mml";

/**
 * Normalize content to MML format
 * If already MML, validates and returns as-is
 * If prose, extracts structure and converts to MML
 */
export function normalizeToMML(
  content: string,
  layer: MemoryLayer,
  importance: number,
  tags: string[],
  acquisitionContext?: { tokens_consumed: number; tool_calls: number },
): NormalizedEntry {
  // Check if already MML
  if (isAlreadyMML(content)) {
    return validateAndPassthrough(content, importance, tags);
  }

  // Classify content type
  const contentType = classifyContent(content);

  // Extract fields based on type
  const extracted = extractFields(content, contentType);

  // Generate heading
  const title = generateHeading(content, contentType, extracted);

  // Build fields dictionary
  const fields: Record<string, string> = {
    what: extracted.what || content.substring(0, 200).trim(),
    ...extracted.fields,
    tags: tags.join(", "),
    importance: importance.toFixed(2),
    created: new Date().toISOString().split("T")[0], // YYYY-MM-DD format
  };

  // Add acquisition context if provided (internal only, stripped from LLM output)
  if (acquisitionContext) {
    fields.acquisition = `${acquisitionContext.tokens_consumed}t, ${acquisitionContext.tool_calls}tc`;
  }

  return {
    title,
    fields,
    tags,
    importance,
  };
}

/**
 * Check if content is already in MML format
 */
function isAlreadyMML(content: string): boolean {
  const trimmed = content.trim();
  return trimmed.startsWith("### ") || trimmed.startsWith("- **");
}

/**
 * Validate MML structure and pass through
 */
function validateAndPassthrough(
  content: string,
  importance: number,
  tags: string[],
): NormalizedEntry {
  // Extract title (first ### line)
  const lines = content.split("\n");
  let title = "Untitled";
  const fields: Record<string, string> = {};

  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith("### ")) {
      title = trimmed.substring(4).trim();
    } else if (trimmed.startsWith("- **")) {
      // Parse key-value pair
      const keyEndIndex = trimmed.indexOf("**:", 4);
      if (keyEndIndex !== -1) {
        const key = trimmed.substring(4, keyEndIndex).trim();
        const value = trimmed.substring(keyEndIndex + 3).trim();
        fields[key] = value;
      }
    }
  }

  // Ensure required fields exist
  if (!fields.what) {
    fields.what = "Structured memory entry";
  }
  if (!fields.tags) {
    fields.tags = tags.join(", ");
  }
  if (!fields.importance) {
    fields.importance = importance.toFixed(2);
  }
  if (!fields.created) {
    fields.created = new Date().toISOString().split("T")[0];
  }

  return {
    title,
    fields,
    tags: fields.tags.split(",").map((t) => t.trim()),
    importance: parseFloat(fields.importance) || importance,
  };
}

/**
 * Classify prose content type
 */
function classifyContent(content: string): ContentType {
  const lower = content.toLowerCase();

  // Decision pattern
  if (
    /\b(decided|deciding|chose|chosen|choosing|going with|selected|selecting|picking|pick)\b/i.test(
      content,
    ) ||
    /\b(vs\.|versus|instead of|rather than)\b/i.test(content)
  ) {
    return "decision";
  }

  // Problem pattern
  if (
    /\b(bug|error|fixed|broken|crash|issue|problem|failure|failed)\b/i.test(
      content,
    ) ||
    /\b(workaround|root cause|discovered|found that)\b/i.test(content)
  ) {
    return "problem";
  }

  // Rule pattern
  if (
    /\b(always|never|must|should|don't|do not|convention|pattern|best practice)\b/i.test(
      content,
    ) ||
    /\b(rule|guideline|standard|procedure)\b/i.test(content)
  ) {
    return "rule";
  }

  return "generic";
}

/**
 * Extract structured fields from prose
 */
function extractFields(
  content: string,
  contentType: ContentType,
): { what: string; fields: Record<string, string> } {
  const fields: Record<string, string> = {};

  switch (contentType) {
    case "decision":
      return extractDecisionFields(content);
    case "problem":
      return extractProblemFields(content);
    case "rule":
      return extractRuleFields(content);
    default:
      return extractGenericFields(content);
  }
}

/**
 * Extract decision-related fields
 */
function extractDecisionFields(content: string): {
  what: string;
  fields: Record<string, string>;
} {
  const fields: Record<string, string> = {};

  // Extract what was decided
  const decisionMatch = content.match(
    /(?:decided|chose|chosen|going with|selected|pick(?:ing|ed)?)\s+(?:to\s+)?([^.!?]+)/i,
  );
  const what = decisionMatch
    ? decisionMatch[1].trim()
    : getFirstSentence(content);

  // Extract why
  const whyMatch = content.match(
    /(?:because|since|due to|reason|why:?)\s+([^.!?]+)/i,
  );
  if (whyMatch) {
    fields.why = whyMatch[1].trim();
  }

  // Extract rejected alternatives (handle multiple sentence patterns)
  let rejectedMatch = content.match(
    /(?:rejected|instead of|rather than)\s+([^.!?]+)/i,
  );
  if (!rejectedMatch) {
    // Try matching "over X" pattern (captures what was rejected)
    rejectedMatch = content.match(/\bover\s+([A-Z][^.!?]*?)(?=\.\s|$)/i);
  }
  if (rejectedMatch && rejectedMatch[1].length < 200) {
    fields.rejected = rejectedMatch[1].trim();
  }

  // Extract constraints
  const constraintMatch = content.match(
    /(?:constraint|requirement|must|need to|have to)\s+([^.!?]+)/i,
  );
  if (constraintMatch && !constraintMatch[1].match(/decide|choose/i)) {
    fields.constraint = constraintMatch[1].trim();
  }

  return { what, fields };
}

/**
 * Extract problem/bug-related fields
 */
function extractProblemFields(content: string): {
  what: string;
  fields: Record<string, string>;
} {
  const fields: Record<string, string> = {};

  // Extract what the problem was
  const problemMatch = content.match(
    /(?:bug|error|issue|problem|crash|failure)\s+(?:in|with|at)?\s*([^.!?]+)/i,
  );
  const what = problemMatch
    ? problemMatch[1].trim()
    : getFirstSentence(content);

  // Extract symptom
  const symptomMatch = content.match(
    /(?:symptom|seeing|shows|displays|causes|results in)\s+([^.!?]+)/i,
  );
  if (symptomMatch) {
    fields.symptom = symptomMatch[1].trim();
  }

  // Extract fix (capture until sentence end, allowing dots in technical terms)
  const fixMatch = content.match(
    /(?:fixed|solved|resolved|fix:|solution:)\s+(.+?)(?=[.!?]\s+[A-Z]|[!?]\s|$)/is,
  );
  if (fixMatch) {
    fields.fix = fixMatch[1].trim().replace(/\.$/, ""); // Remove trailing period if present
  }

  // Extract root cause
  const rootCauseMatch = content.match(
    /(?:root cause|caused by|due to|because of)\s+([^.!?]+)/i,
  );
  if (rootCauseMatch && !fields.fix) {
    fields["root-cause"] = rootCauseMatch[1].trim();
  }

  // Extract workaround
  const workaroundMatch = content.match(/(?:workaround)\s+([^.!?]+)/i);
  if (workaroundMatch) {
    fields.workaround = workaroundMatch[1].trim();
  }

  // Extract file reference (handle both ClassName.Method() and ClassName.ext patterns)
  let fileMatch = content.match(
    /(?:in|at|file)\s+([A-Z][a-zA-Z0-9_]*)\.([a-z]{2,4}\b|[A-Z][a-zA-Z0-9_]*)/,
  );
  if (fileMatch) {
    // If second part looks like a method (starts with capital), assume .cs file
    if (fileMatch[2] && /^[A-Z]/.test(fileMatch[2])) {
      fields.file = fileMatch[1] + ".cs";
    } else {
      fields.file = fileMatch[1] + "." + fileMatch[2];
    }
  }

  return { what, fields };
}

/**
 * Extract rule/procedure-related fields
 */
function extractRuleFields(content: string): {
  what: string;
  fields: Record<string, string>;
} {
  const fields: Record<string, string> = {};

  // Extract what the rule is about
  const what = getFirstSentence(content);

  // Extract do (positive rules)
  const doMatch = content.match(
    /(?:always|must|should|do)\s+([^.!?]+?)(?=\.|!|\?|never|don't|do not|$)/i,
  );
  if (doMatch) {
    fields.do = doMatch[1].trim();
  }

  // Extract don't (negative rules)
  const dontMatch = content.match(
    /(?:never|don't|do not|should not|must not)\s+([^.!?]+)/i,
  );
  if (dontMatch) {
    fields.dont = dontMatch[1].trim();
  }

  // Extract format/pattern if mentioned (capture what comes before format keyword)
  const formatMatch = content.match(
    /(?:follow|use|uses|using)\s+(?:the\s+)?([\w\s]+?)\s+(?:format|pattern|structure)/i,
  );
  if (formatMatch) {
    fields.format = formatMatch[1].trim();
  }

  return { what, fields };
}

/**
 * Extract generic fields (fallback)
 */
function extractGenericFields(content: string): {
  what: string;
  fields: Record<string, string>;
} {
  const what =
    content.length > 200
      ? getFirstSentence(content)
      : content.trim() || "Memory entry";
  return { what, fields: {} };
}

/**
 * Generate appropriate heading based on content type
 */
function generateHeading(
  content: string,
  contentType: ContentType,
  extracted: { what: string; fields: Record<string, string> },
): string {
  switch (contentType) {
    case "decision":
      return generateDecisionHeading(content, extracted);
    case "problem":
      return generateProblemHeading(content, extracted);
    case "rule":
      return generateRuleHeading(content, extracted);
    default:
      return generateGenericHeading(extracted.what);
  }
}

/**
 * Generate heading for decisions
 * Format: [Entity] — [Role/Category]
 */
function generateDecisionHeading(
  content: string,
  extracted: { what: string; fields: Record<string, string> },
): string {
  const what = extracted.what;

  // Extract entity (technology name, tool name, etc.)
  const entityMatch = what.match(/\b([A-Z][a-zA-Z0-9]*(?:\s+\d+)?)\b/);
  const entity = entityMatch ? entityMatch[1] : "";

  // Extract role/purpose
  const roleMatch = content.match(
    /(?:as|for|to be)\s+(?:the\s+)?([a-z\s]+?)(?:\s+(?:because|since|due to)|\.|\n|$)/i,
  );
  const role = roleMatch ? roleMatch[1].trim() : "";

  if (entity && role) {
    return `${entity} — ${capitalize(role)}`;
  } else if (entity) {
    return `${entity} — Decision`;
  } else {
    return truncateToHeading(what);
  }
}

/**
 * Generate heading for problems
 * Format: [Problem description] in [Location]
 */
function generateProblemHeading(
  content: string,
  extracted: { what: string; fields: Record<string, string> },
): string {
  const what = extracted.what;
  const location = extracted.fields.file || "";

  if (location) {
    return `${truncateToHeading(what)} in ${location}`;
  } else {
    // Try to extract location from content
    const locationMatch = content.match(
      /in\s+([A-Z][a-zA-Z0-9_]*(?:\.[a-z]{2,4})?)/,
    );
    if (locationMatch) {
      return `${truncateToHeading(what)} in ${locationMatch[1]}`;
    }
  }

  return truncateToHeading(what);
}

/**
 * Generate heading for rules
 * Format: [Subject of rule]
 */
function generateRuleHeading(
  content: string,
  extracted: { what: string; fields: Record<string, string> },
): string {
  const what = extracted.what;

  // Extract subject (what the rule applies to)
  const subjectMatch = what.match(
    /^(?:always|never|must|should)?\s*(.+?)(?:\s+(?:with|using|by)|$)/i,
  );
  const subject = subjectMatch ? subjectMatch[1].trim() : what;

  return truncateToHeading(subject);
}

/**
 * Generate generic heading
 */
function generateGenericHeading(what: string): string {
  return truncateToHeading(what);
}

/**
 * Get first sentence from content
 */
function getFirstSentence(content: string): string {
  if (!content || content.trim() === "") {
    return "Memory entry";
  }

  const sentences = content.split(/[.!?]+/);
  const firstSentence = sentences[0]?.trim() || content.trim();
  return firstSentence.length > 200
    ? firstSentence.substring(0, 200).trim() + "..."
    : firstSentence;
}

/**
 * Truncate text to appropriate heading length
 * Max 60 characters, break on word boundary
 */
function truncateToHeading(text: string): string {
  const cleaned = text.trim().replace(/\s+/g, " ");

  if (!cleaned) {
    return "Memory Entry";
  }

  if (cleaned.length <= 60) {
    return capitalize(cleaned);
  }

  // Find last word boundary before 60 chars
  const truncated = cleaned.substring(0, 60);
  const lastSpace = truncated.lastIndexOf(" ");

  if (lastSpace > 40) {
    return capitalize(truncated.substring(0, lastSpace));
  }

  return capitalize(truncated.substring(0, 57)) + "...";
}

/**
 * Capitalize first letter
 */
function capitalize(text: string): string {
  if (!text) return text;
  return text.charAt(0).toUpperCase() + text.slice(1);
}
