import type {
  OntologyWriteClass,
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";
import type { DocumentWriteResponse } from "../api/documentWriteModels";
import { createClientId } from "./clientId";
import type { ResourceDraft } from "./resourceDraftModels";

function localName(id: string): string {
  const parts = id.split(/[\/#]/);
  return parts.at(-1)?.toLowerCase() ?? id.toLowerCase();
}

function isUriProperty(property: OntologyWriteProperty): boolean {
  const name = localName(property.id);
  return name === "uri" || name.endsWith("-uri") || name.endsWith("_uri") ||
    /(^|\s)uri($|\s)/i.test(property.label);
}

export function documentClasses(schema: OntologyWriteSchema): OntologyWriteClass[] {
  return schema.classes.filter(type => uriProperties(type).length > 0);
}

export function uriProperties(type: OntologyWriteClass | null): OntologyWriteProperty[] {
  return type?.properties.filter(property =>
    property.kind === "literal" &&
    property.options.length === 0 &&
    isUriProperty(property)
  ) ?? [];
}

export function preferredUriProperty(
  type: OntologyWriteClass | null
): OntologyWriteProperty | null {
  const properties = uriProperties(type);
  return properties.find(property => localName(property.id) === "uri")
    ?? properties[0]
    ?? null;
}

export function documentResourceDraft(
  upload: DocumentWriteResponse,
  typeId: string,
  uriPredicate: string
): ResourceDraft {
  return {
    typeId,
    resourceId: "",
    cassetteId: upload.cassetteId,
    properties: [{
      rowId: createClientId(),
      predicate: uriPredicate,
      value: upload.documentUri,
      kind: "literal",
      language: "",
      dataType: ""
    }]
  };
}
