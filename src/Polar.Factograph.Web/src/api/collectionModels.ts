export interface CollectionItem {
  membershipResourceId: string;
  resourceId: string;
  displayName: string;
  type: string | null;
  typeLabel: string | null;
  membershipCassetteId: string;
  resourceCassetteId: string;
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
