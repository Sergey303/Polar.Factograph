import type { OntologyWriteProperty } from "../api/ontologyModels";
import type { ResourcePortrait } from "../api/models";
import type { ResourceWriteRequest } from "../api/resourceWriteModels";
import type {
  ResourceDraft,
  ResourcePropertyDraft
} from "./resourceDraftModels";

function rowId(): string {
  return crypto.randomUUID();
}

export function emptyResourceDraft(cassetteId: string): ResourceDraft {
  return {
    typeId: "",
    resourceId: "",
    cassetteId,
    properties: []
  };
}

export function resourceDraftFromPortrait(
  portrait: ResourcePortrait,
  cassetteId: string
): ResourceDraft {
  const literals: ResourcePropertyDraft[] = portrait.literals.map(field => ({
    rowId: rowId(),
    predicate: field.predicate,
    value: field.value,
    kind: "literal",
    language: field.language ?? "",
    dataType: field.dataType ?? ""
  }));
  const links: ResourcePropertyDraft[] = portrait.directLinks.map(link => ({
    rowId: rowId(),
    predicate: link.predicate,
    value: link.targetResourceId,
    kind: "resource",
    language: "",
    dataType: ""
  }));
  return {
    typeId: portrait.type ?? "",
    resourceId: portrait.resourceId,
    cassetteId,
    properties: [...literals, ...links]
  };
}

export function newPropertyDraft(
  property: OntologyWriteProperty
): ResourcePropertyDraft {
  return {
    rowId: rowId(),
    predicate: property.id,
    value: property.options[0]?.value ?? "",
    kind: property.kind,
    language: "",
    dataType: ""
  };
}

export function validateResourceDraft(draft: ResourceDraft): string | null {
  if (draft.typeId.trim().length === 0) return "Выберите тип ресурса.";
  if (draft.cassetteId.trim().length === 0) return "Выберите кассету записи.";
  if (draft.properties.some(item => item.predicate.trim().length === 0)) {
    return "У каждого значения должно быть свойство.";
  }
  if (draft.properties.some(item => item.value.trim().length === 0)) {
    return "Значения свойств не могут быть пустыми.";
  }
  return null;
}

export function toResourceWriteRequest(
  draft: ResourceDraft
): ResourceWriteRequest {
  return {
    typeId: draft.typeId.trim(),
    resourceId: draft.resourceId.trim() || null,
    cassetteId: draft.cassetteId.trim() || null,
    properties: draft.properties.map(item => ({
      predicate: item.predicate.trim(),
      value: item.kind === "resource" ? item.value.trim() : item.value,
      kind: item.kind,
      language: item.kind === "literal" ? item.language.trim() || null : null,
      dataType: item.kind === "literal" ? item.dataType.trim() || null : null
    }))
  };
}