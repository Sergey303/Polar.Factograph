import type {
  OntologyWriteClass,
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";

export interface OntologyRelationRole {
  key: string;
  relationType: OntologyWriteClass;
  anchorProperty: OntologyWriteProperty;
  label: string;
}

export function ontologyAncestorsAndSelf(
  schema: OntologyWriteSchema,
  typeId: string
): string[] {
  const result: string[] = [];
  const visited = new Set<string>();
  let current = schema.classes.find(item => item.id === typeId) ?? null;

  while (current !== null && !visited.has(current.id)) {
    result.push(current.id);
    visited.add(current.id);
    const parentClassId = current.parentClassId;
    current = parentClassId === null
      ? null
      : schema.classes.find(item => item.id === parentClassId) ?? null;
  }

  return result;
}

export function ontologyTypeMatchesRanges(
  schema: OntologyWriteSchema,
  typeId: string | null,
  ranges: string[]
): boolean {
  if (typeId === null || ranges.length === 0) return false;
  const ancestors = ontologyAncestorsAndSelf(schema, typeId);
  return ranges.some(range => ancestors.includes(range));
}

export function entityTypesMatchingRanges(
  schema: OntologyWriteSchema,
  ranges: string[]
): OntologyWriteClass[] {
  return schema.classes
    .filter(type =>
      type.isEntityType &&
      !type.isAbstract &&
      ontologyTypeMatchesRanges(schema, type.id, ranges))
    .sort((left, right) => left.label.localeCompare(right.label, "ru"));
}

export function relationRolesForType(
  schema: OntologyWriteSchema,
  currentTypeId: string
): OntologyRelationRole[] {
  const roles: OntologyRelationRole[] = [];

  for (const relationType of schema.classes) {
    if (relationType.isAbstract || relationType.isEntityType) continue;

    for (const property of relationType.properties) {
      if (
        property.kind !== "resource" ||
        !ontologyTypeMatchesRanges(schema, currentTypeId, property.ranges)
      ) {
        continue;
      }

      const roleLabel = property.inverseLabel ?? property.label;
      roles.push({
        key: `${relationType.id}\n${property.id}`,
        relationType,
        anchorProperty: property,
        label: `${relationType.label}: ${roleLabel}`
      });
    }
  }

  return roles.sort((left, right) => left.label.localeCompare(right.label, "ru"));
}
