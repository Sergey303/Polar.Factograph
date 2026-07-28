import type { ResourceDraft } from "./resourceDraftModels";
import { newPropertyDraft } from "./resourceDraftFactory";
import type { OntologyRelationRole } from "./ontologyRelations";

export interface RelationDraftResult {
  draft: ResourceDraft;
  anchorRowId: string;
}

export function relationDraft(
  role: OntologyRelationRole,
  currentResourceId: string,
  cassetteId: string
): RelationDraftResult {
  const properties = role.relationType.properties
    .filter(property => property.isEssential || property.id === role.anchorProperty.id)
    .map(newPropertyDraft);
  const anchor = properties.find(item => item.predicate === role.anchorProperty.id);
  if (anchor === undefined) {
    throw new Error(`Anchor property was not found: ${role.anchorProperty.id}`);
  }
  anchor.value = currentResourceId;

  return {
    draft: {
      typeId: role.relationType.id,
      resourceId: "",
      cassetteId,
      properties
    },
    anchorRowId: anchor.rowId
  };
}
