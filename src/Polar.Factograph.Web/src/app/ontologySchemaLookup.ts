import type {
  OntologyWriteClass,
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";

export function findWriteClass(
  schema: OntologyWriteSchema | null,
  typeId: string
): OntologyWriteClass | null {
  return schema?.classes.find(item => item.id === typeId) ?? null;
}

export function findWriteProperty(
  schema: OntologyWriteSchema | null,
  typeId: string,
  predicate: string
): OntologyWriteProperty | null {
  return findWriteClass(schema, typeId)?.properties.find(
    item => item.id === predicate
  ) ?? null;
}