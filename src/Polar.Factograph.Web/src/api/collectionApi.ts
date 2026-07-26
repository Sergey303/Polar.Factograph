import { requestJson, requestJsonBody } from "./http";
import type {
  CollectionContents,
  CollectionMutationResponse
} from "./collectionModels";

function parameters(values: Record<string, string | number>): string {
  const result = new URLSearchParams();
  for (const [name, value] of Object.entries(values)) {
    result.set(name, String(value));
  }
  return result.toString();
}

export const collectionApi = {
  getItems(
    collectionId: string,
    token: string,
    signal?: AbortSignal
  ): Promise<CollectionContents> {
    const query = parameters({ id: collectionId, limit: 100, lang: "ru" });
    return requestJson<CollectionContents>(
      `/api/collections/items?${query}`,
      token,
      signal
    );
  },

  addItem(
    collectionId: string,
    resourceId: string,
    cassetteId: string | null,
    token: string
  ): Promise<CollectionMutationResponse> {
    return requestJsonBody<CollectionMutationResponse>(
      "/api/collections/items",
      "POST",
      { collectionId, resourceId, cassetteId },
      token
    );
  },

  removeItem(
    membershipResourceId: string,
    collectionId: string,
    resourceId: string,
    cassetteId: string | null,
    token: string
  ): Promise<CollectionMutationResponse> {
    return requestJsonBody<CollectionMutationResponse>(
      "/api/collections/items/remove",
      "POST",
      { membershipResourceId, collectionId, resourceId, cassetteId },
      token
    );
  }
};
