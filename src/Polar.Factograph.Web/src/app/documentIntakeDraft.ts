import type {
  OntologyWriteClass,
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";
import type { DocumentWriteResponse } from "../api/documentWriteModels";
import type { ResourceDraft } from "./resourceDraftModels";

function localName(id: string): string {
  const parts = id.split(/[\/#]/);
  return parts.at(-1)?.toLowerCase() ?? id.toLowerCase();
}

export function documentClasses(schema: OntologyWriteSchema): OntologyWriteClass[] {
  return schema.classes.filter(type => literalProperties(type).length > 0);
}

export function literalProperties(type: OntologyWriteClass | null): OntologyWriteProperty[] {
  return type?.properties.filter(property =>
    property.kind === "literal" && property.options.length === 0
  ) ?? [];
}

export function preferredUriProperty(
  type: OntologyWriteClass | null
): OntologyWriteProperty | null {
  const properties = literalProperties(type);
  return properties.find(property => localName(property.id) === "uri")
    ?? properties.find(property => /(^|\s)uri($|\s)/i.test(property.label))
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
      rowId: crypto.randomUUID(),
      predicate: uriPredicate,
      value: upload.documentUri,
      kind: "literal",
      language: "",
      dataType: ""
    }]
  };
}
