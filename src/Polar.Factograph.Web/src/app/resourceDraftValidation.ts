import type { OntologyWriteSchema } from "../api/ontologyModels";
import { findWriteClass } from "./ontologySchemaLookup";
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

  // Legacy records may contain properties that are absent from the current
  // authoring ontology. The editor preserves those values instead of making
  // an otherwise valid historical record impossible to revise. New properties
  // are still selected from the write schema by the editor UI.
  return null;
}
