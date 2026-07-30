export interface CollectionItem {
  resourceId: string;
  displayName: string;
  type: string | null;
  typeLabel: string | null;
  membershipResourceId: string | null;
  membershipCassetteId: string | null;
}

export interface CollectionContents {
  collectionId: string;
  items: CollectionItem[];
}

export interface CollectionMutationResponse {
  membershipResourceId: string;
  collectionId: string;
  resourceId: string;
  cassetteId: string;
  modifiedAtUtc: string;
  indexReady: boolean;
  generationId: string | null;
}
