import type { ResourceSearchResult, SearchEvidence } from "./models";

function mergeEvidence(
  first: SearchEvidence[],
  second: SearchEvidence[]
): SearchEvidence[] {
  const result = [...first];
  const known = new Set(first.map(evidence => evidence.value));
  for (const evidence of second) {
    if (known.has(evidence.value)) continue;
    known.add(evidence.value);
    result.push(evidence);
  }
  return result;
}

export function mergeSearchResults(
  names: ResourceSearchResult[],
  words: ResourceSearchResult[],
  limit = 50
): ResourceSearchResult[] {
  const merged = new Map<string, ResourceSearchResult>();
  for (const result of [...names, ...words]) {
    const current = merged.get(result.resourceId);
    if (current === undefined) {
      merged.set(result.resourceId, result);
      continue;
    }

    merged.set(result.resourceId, {
      ...current,
      score: Math.max(current.score, result.score),
      matches: mergeEvidence(current.matches, result.matches)
    });
  }
  return [...merged.values()].slice(0, limit);
}
