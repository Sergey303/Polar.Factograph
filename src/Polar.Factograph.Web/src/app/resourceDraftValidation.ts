import type { OntologyWriteSchema } from "../api/ontologyModels";
import { findWriteClass, findWriteProperty } from "./ontologySchemaLookup";
import type { ResourceDraft } from "./resourceDraftModels";

export function validateResourceDraft(
  draft: ResourceDraft,
  schema: OntologyWriteSchema
): string | null {
  if (draft.typeId.trim().length === 0) return "Выберите тип ресурса.";
  if (findWriteClass(schema, draft.typeId) === null) {
    return "Тип ресурса отсутствует в текущей схеме записи.";
  }
  if (draft.cassetteId.trim().length === 0) return "Выберите кассету записи.";
  if (draft.properties.some(item => item.predicate.trim().length === 0)) {
    return "У каждого значения должно быть свойство.";
  }
  if (draft.properties.some(item => item.value.trim().length === 0)) {
    return "Значения свойств не могут быть пустыми.";
  }

  const unsupported = draft.properties.find(item =>
    findWriteProperty(schema, draft.typeId, item.predicate) === null);
  if (unsupported !== undefined) {
    return `Свойство ${unsupported.predicate} не разрешено текущей схемой записи.`;
  }
  return null;
}