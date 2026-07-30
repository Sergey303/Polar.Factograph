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
  canAdmin: boolean;
  cassettes: ProjectCassetteOverview[];
  defaultWriteCassetteId: string | null;
}

export interface SearchEvidence {
  value: string;
}

export interface ResourceSearchResult {
  resourceId: string;
  displayName: string;
  type: string | null;
  typeLabel: string | null;
  score: number;
  matches: SearchEvidence[];
}

export interface OntologyClassSearchSuggestion {
  classId: string;
  label: string;
  exactMatch: boolean;
  isAbstract: boolean;
}

export interface ResourceTypeSearchPage {
  classId: string;
  label: string;
  total: number;
  offset: number;
  limit: number;
  results: ResourceSearchResult[];
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
}

export interface ResourceProvenance {
  sourceCassetteId: string;
  sourceRecordId: string | null;
  sourceFogPath: string | null;
  modifiedAt: string | null;
}

export interface ResourcePortrait {
  resourceId: string;
  type: string | null;
  typeLabel: string | null;
  literals: PresentedLiteral[];
  directLinks: PresentedDirectLink[];
  inverseLinks: PresentedInverseLink[];
  provenance: ResourceProvenance | null;
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
  hasDocument?: boolean;
}

export interface SemanticRelationMember {
  resourceId: string;
  displayName: string;
  type: string | null;
  typeLabel: string | null;
  roleLabel: string | null;
  documentUri: string | null;
  hasDocument?: boolean;
}

export interface SemanticRelationValue {
  label: string;
  value: string;
}

export interface SemanticRelationEntry {
  key: string;
  title: string;
  relationResourceId: string | null;
  relationType: string | null;
  relationTypeLabel: string | null;
  groupKey: string;
  groupLabel: string;
  displayDate: string | null;
  sortDate: string | null;
  documentUri: string | null;
  members: SemanticRelationMember[];
  values: SemanticRelationValue[];
}

export interface SemanticPhotoCard {
  resourceId: string;
  displayName: string;
  documentUri: string | null;
  contextResourceId: string | null;
  contextLabel: string | null;
  displayDate?: string | null;
  sortDate?: string | null;
  hasDocument?: boolean;
}

export interface SemanticResourcePage {
  requestedResourceId: string;
  portrait: ResourcePortrait;
  photos: SemanticPhotoCard[];
  participants: SemanticResourceLink[];
  organizations: SemanticResourceLink[];
  collections: SemanticResourceLink[];
  relatedResources: SemanticResourceLink[];
  links?: SemanticResourceLink[];
  entries?: SemanticRelationEntry[];
}

export interface DocumentLocation {
  cassetteId: string | null;
  cassetteName: string | null;
  originalAvailable: boolean;
  iconPreviewAvailable: boolean;
  smallPreviewAvailable: boolean;
  mediumPreviewAvailable: boolean;
  normalPreviewAvailable: boolean;
}

export type DocumentVariant = "original" | "icon" | "small" | "medium" | "normal";
