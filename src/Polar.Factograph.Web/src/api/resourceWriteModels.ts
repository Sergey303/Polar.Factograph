export interface ResourceWriteProperty {
  predicate: string;
  value: string;
  kind: "literal" | "resource";
  language: string | null;
  dataType: string | null;
}

export interface ResourceWriteRequest {
  typeId: string;
  properties: ResourceWriteProperty[];
  resourceId: string | null;
  cassetteId: string | null;
}

export interface ResourceWriteResponse {
  resourceId: string;
  cassetteId: string;
  modifiedAtUtc: string;
  indexReady: boolean;
  generationId: string | null;
}