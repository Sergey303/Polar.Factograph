import { useEffect, useState } from "react";
import type { OntologyWriteProperty } from "../api/ontologyModels";
import { newPropertyDraft } from "./resourceDraftFactory";
import type {
  ResourceDraft,
  ResourcePropertyDraft
} from "./resourceDraftModels";

export function useResourceDraft(initial: ResourceDraft) {
  const [draft, setDraft] = useState(initial);

  useEffect(() => setDraft(initial), [initial]);

  function setType(typeId: string): void {
    setDraft(current => current.typeId === typeId
      ? current
      : { ...current, typeId, properties: [] });
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