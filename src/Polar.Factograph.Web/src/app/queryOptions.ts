import { queryOptions } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";
import { collectionApi } from "../api/collectionApi";
import { factographApi } from "../api/factographApi";
import type { DocumentVariant } from "../api/models";
import { ontologyApi } from "../api/ontologyApi";

export function projectQueryOptions(token: string) {
  return queryOptions({
    queryKey: ["project", token] as const,
    queryFn: ({ signal }) => factographApi.getProject(token, signal)
  });
}

export function searchQueryOptions(query: string, token: string) {
  return queryOptions({
    queryKey: ["search", token, query] as const,
    queryFn: ({ signal }) => factographApi.search(query, token, signal)
  });
}

export function resourcePageQueryOptions(resourceId: string, token: string) {
  return queryOptions({
    queryKey: ["semantic-resource-page", token, resourceId] as const,
    queryFn: ({ signal }) => factographApi.getResourcePage(resourceId, token, signal)
  });
}

export function portraitQueryOptions(resourceId: string, token: string) {
  return queryOptions({
    queryKey: ["resource-portrait", token, resourceId] as const,
    queryFn: ({ signal }) => factographApi.getPortrait(resourceId, token, signal)
  });
}

export function documentLocationQueryOptions(uri: string, token: string) {
  return queryOptions({
    queryKey: ["document-location", token, uri] as const,
    queryFn: ({ signal }) => factographApi.getDocumentLocation(uri, token, signal)
  });
}

export function documentBlobQueryOptions(
  uri: string,
  variant: DocumentVariant,
  token: string
) {
  return queryOptions({
    queryKey: ["document-blob", token, uri, variant] as const,
    queryFn: ({ signal }) => factographApi.getDocumentBlob(uri, variant, token, signal)
  });
}

export function ontologySchemaQueryOptions(token: string) {
  return queryOptions({
    queryKey: ["ontology-write-schema", token] as const,
    queryFn: ({ signal }) => ontologyApi.getWriteSchema(token, signal),
    staleTime: 5 * 60 * 1000
  });
}

export function collectionContentsQueryOptions(collectionId: string, token: string) {
  return queryOptions({
    queryKey: ["collection-contents", token, collectionId] as const,
    queryFn: ({ signal }) => collectionApi.getItems(collectionId, token, signal)
  });
}

export function adminIndexStatusQueryOptions(token: string) {
  return queryOptions({
    queryKey: ["admin-index-status", token] as const,
    queryFn: ({ signal }) => adminApi.getIndexStatus(token, signal)
  });
}

export function adminPreviewStatusQueryOptions(token: string) {
  return queryOptions({
    queryKey: ["admin-preview-status", token] as const,
    queryFn: ({ signal }) => adminApi.getPreviewStatus(token, signal)
  });
}

export function materializationSummaryQueryOptions(token: string) {
  return queryOptions({
    queryKey: ["materialization-summary", token] as const,
    queryFn: ({ signal }) => adminApi.getMaterializationSummary(token, signal)
  });
}
