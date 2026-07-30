export interface ApiError {
  code: string;
  message: string;
}

export interface ProjectCassetteOverview {
  id: string;
  name: string;
  allowWrite: boolean;
  rights: string[];
}

export interface ProjectOverview {
  projectId: string;
  name: string;
  userId: string;
  projectRights: string[];
  cassettes: ProjectCassetteOverview[];
  defaultWriteCassetteId: string | null;
}

export interface SearchEvidence {
  predicate: string;
  value: string;
  language: string | null;
}

export interface ResourceSearchResult {
  resourceId: string;
  displayName: string;
  type: string | null;
  typeLabel: string | null;
  score: number;
  sourceCassetteId: string;
  matches: SearchEvidence[];
}

export interface PotentialDuplicateResource {
  resourceId: string;
  displayName: string;
  type: string | null;
  typeLabel: string | null;
  predicate: string;
  matchedValue: string;
  alternativeWriting: boolean;
}

export interface PresentedLiteral {
  predicate: string;
  label: string;
  value: string;
  displayValue: string;
  language: string | null;
  dataType: string | null;
}

export interface PresentedDirectLink {
  predicate: string;
  label: string;
  targetResourceId: string;
}

export interface PresentedInverseLink {
  predicate: string;
  label: string;
  sourceResourceId: string;
  sourceCassetteId: string;
}

export interface ResourceProvenance {
  sourceRecordId: string;
  sourceCassetteId: string;
  sourceFogPath: string;
  modifiedAt: string;
}

export interface ResourcePortrait {
  resourceId: string;
  type: string | null;
  typeLabel: string | null;
  literals: PresentedLiteral[];
  directLinks: PresentedDirectLink[];
  inverseLinks: PresentedInverseLink[];
  provenance: ResourceProvenance;
}

export interface SemanticResourceLink {
  resourceId: string;
  displayName: string;
  type: string | null;
  typeLabel: string | null;
  relationLabel: string;
  relationResourceId?: string | null;
  documentUri?: string | null;
  displayDate?: string | null;
  sortDate?: string | null;
  groupKey?: string | null;
  groupLabel?: string | null;
}

export interface SemanticPhotoCard {
  resourceId: string;
  displayName: string;
  documentUri: string | null;
  contextResourceId: string | null;
  contextLabel: string | null;
  displayDate?: string | null;
  sortDate?: string | null;
}

export interface SemanticResourcePage {
  requestedResourceId: string;
  portrait: ResourcePortrait;
  photos: SemanticPhotoCard[];
  participants: SemanticResourceLink[];
  organizations: SemanticResourceLink[];
  collections: SemanticResourceLink[];
  relatedResources: SemanticResourceLink[];
}

export interface DocumentLocation {
  cassetteId: string;
  cassetteName: string;
  documentUri: string;
  folderName: string;
  documentNumber: string;
  originalAvailable: boolean;
  iconPreviewAvailable: boolean;
  smallPreviewAvailable: boolean;
  mediumPreviewAvailable: boolean;
  normalPreviewAvailable: boolean;
}

export type DocumentVariant = "original" | "icon" | "small" | "medium" | "normal";
