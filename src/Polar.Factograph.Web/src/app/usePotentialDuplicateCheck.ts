import { useCallback, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { PotentialDuplicateResource } from "../api/models";
import type { OntologyWriteSchema } from "../api/ontologyModels";
import type { ResourceDraft } from "./resourceDraftModels";

interface DuplicateField {
  predicate: string;
  value: string;
}

function duplicateFields(
  draft: ResourceDraft,
  schema: OntologyWriteSchema
): DuplicateField[] {
  const selectedClass = schema.classes.find(type => type.id === draft.typeId);
  if (!selectedClass) return [];

  const properties = new Map(
    selectedClass.properties.map(property => [property.id, property] as const)
  );
  const unique = new Map<string, DuplicateField>();
  for (const row of draft.properties) {
    const value = row.value.trim();
    const property = properties.get(row.predicate);
    if (
      row.kind !== "literal" ||
      value.length === 0 ||
      value.length > 512 ||
      property?.kind !== "literal" ||
      property.options.length > 0
    ) {
      continue;
    }

    const key = `${row.predicate}\n${value}`;
    unique.set(key, { predicate: row.predicate, value });
    if (unique.size === 5) break;
  }
  return [...unique.values()];
}

export function usePotentialDuplicateCheck(
  token: string,
  schema: OntologyWriteSchema
) {
  const [checking, setChecking] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [candidates, setCandidates] = useState<PotentialDuplicateResource[]>([]);

  const reset = useCallback(() => {
    setError(null);
    setCandidates([]);
  }, []);

  const check = useCallback(async (draft: ResourceDraft): Promise<boolean> => {
    if (draft.resourceId.trim().length > 0) {
      reset();
      return true;
    }

    const fields = duplicateFields(draft, schema);
    if (fields.length === 0) {
      reset();
      return true;
    }

    setChecking(true);
    setError(null);
    setCandidates([]);
    try {
      const results = await Promise.all(fields.map(field =>
        factographApi.findPotentialDuplicates(
          draft.typeId,
          field.predicate,
          field.value,
          token
        )));
      const merged = new Map<string, PotentialDuplicateResource>();
      for (const candidate of results.flat()) {
        const existing = merged.get(candidate.resourceId);
        if (
          existing === undefined ||
          existing.alternativeWriting && !candidate.alternativeWriting
        ) {
          merged.set(candidate.resourceId, candidate);
        }
      }

      const next = [...merged.values()]
        .sort((left, right) =>
          Number(left.alternativeWriting) - Number(right.alternativeWriting) ||
          left.displayName.localeCompare(right.displayName, "ru") ||
          left.resourceId.localeCompare(right.resourceId, "ru"))
        .slice(0, 10);
      setCandidates(next);
      return next.length === 0;
    } catch (reason) {
      setError(errorText(reason));
      return false;
    } finally {
      setChecking(false);
    }
  }, [reset, schema, token]);

  return { checking, error, candidates, check, reset };
}
