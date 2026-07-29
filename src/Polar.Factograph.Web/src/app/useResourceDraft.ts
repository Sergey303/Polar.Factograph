import { useEffect, useState } from "react";
import type { OntologyWriteProperty } from "../api/ontologyModels";
import { newPropertyDraft } from "./resourceDraftFactory";
import type {
  ResourceDraft,
  ResourcePropertyDraft
} from "./resourceDraftModels";

function withInitialValue(
  property: OntologyWriteProperty,
  initialValues: Readonly<Record<string, string>>
): ResourcePropertyDraft {
  const draft = newPropertyDraft(property);
  const value = initialValues[property.id]?.trim();
  return value ? { ...draft, value } : draft;
}

export function useResourceDraft(initial: ResourceDraft) {
  const [draft, setDraft] = useState(initial);

  useEffect(() => setDraft(initial), [initial]);

  function setType(
    typeId: string,
    properties: OntologyWriteProperty[] = [],
    initialValues: Readonly<Record<string, string>> = {}
  ): void {
    setDraft(current => {
      if (current.typeId === typeId) return current;

      const allowed = new Map(properties.map(property => [property.id, property] as const));
      const compatible = current.properties
        .filter(row => allowed.has(row.predicate))
        .map(row => {
          const value = initialValues[row.predicate]?.trim();
          return row.value.length === 0 && value ? { ...row, value } : row;
        });
      const existingPredicates = new Set(compatible.map(row => row.predicate));
      const requiredOrPrefilled = properties
        .filter(property =>
          !existingPredicates.has(property.id) &&
          (property.isEssential || Boolean(initialValues[property.id]?.trim())))
        .map(property => withInitialValue(property, initialValues));

      return {
        ...current,
        typeId,
        properties: [...compatible, ...requiredOrPrefilled]
      };
    });
  }

  function setField(field: "resourceId" | "cassetteId", value: string): void {
    setDraft(current => ({ ...current, [field]: value }));
  }

  function addProperty(property: OntologyWriteProperty): void {
    setDraft(current => ({
      ...current,
      properties: [...current.properties, newPropertyDraft(property)]
    }));
  }

  function updateProperty(
    rowId: string,
    changes: Partial<ResourcePropertyDraft>
  ): void {
    setDraft(current => ({
      ...current,
      properties: current.properties.map(item =>
        item.rowId === rowId ? { ...item, ...changes } : item)
    }));
  }

  function removeProperty(rowId: string): void {
    setDraft(current => ({
      ...current,
      properties: current.properties.filter(item => item.rowId !== rowId)
    }));
  }

  return {
    draft,
    setType,
    setField,
    addProperty,
    updateProperty,
    removeProperty
  };
}
