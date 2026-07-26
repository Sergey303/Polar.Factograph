export type ResourceValueKind = "literal" | "resource";

export interface ResourcePropertyDraft {
  rowId: string;
  predicate: string;
  value: string;
  kind: ResourceValueKind;
  language: string;
  dataType: string;
}

export interface ResourceDraft {
  typeId: string;
  resourceId: string;
  cassetteId: string;
  properties: ResourcePropertyDraft[];
}