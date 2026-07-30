import { queryOptions } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";
import { collectionApi } from "../api/collectionApi";
import { factographApi } from "../api/factographApi";
import type { DocumentVariant } from "../api/models";
import { ontologyApi } from "../api/ontologyApi";

export const queryKeys = {
  project: (token: string) => ["project", token] as const,
  search: (token: string, query?: string) =>
    query === undefined
      ? ["search", token] as const
      : ["search", token, query] as const,
  searchClasses: (token: string, query: string) =>
    ["search-classes", token, query] as const,
  searchByType: (token: string, classId: string, offset: number) =>
    ["search-by-type", token, classId, offset] as const,
  resourcePage: (token: string, resourceId: string) =>
    ["semantic-resource-page", token, resourceId] as const,
  portrait: (token: string, resourceId: string) =>
    ["resource-portrait", token, resourceId] as const,
  documentLocation: (token: string, uri: string) =>
    ["document-location", token, uri] as const,
  documentBlob: (token: string, uri: string, variant?: DocumentVariant) =>
    variant === undefined
      ? ["document-blob", token, uri] as const
      : ["document-blob", token, uri, variant] as const,
  ontologySchema: (token: string) => ["ontology-write-schema", token] as const,
  collectionContents: (token: string, collectionId?: string) =>
    collectionId === undefined
      ? ["collection-contents", token] as const
      : ["collection-contents", token, collectionId] as const,
  adminIndexStatus: (token: string) => ["admin-index-status", token] as const,
  adminPreviewStatus: (token: string) => ["admin-preview-status", token] as const,
  materializationSummary: (token: string) => ["materialization-summary", token] as const
};

export function projectQueryOptions(token: string) {
  return queryOptions({
    queryKey: queryKeys.project(token),
    queryFn: ({ signal }) => factographApi.getProject(token, signal)
  });
}

export function searchQueryOptions(query: string, token: string) {
  return queryOptions({
    queryKey: queryKeys.search(token, query),
    queryFn: ({ signal }) => factographApi.search(query, token, signal)
  });
}

export function searchClassesQueryOptions(query: string, token: string) {
  return queryOptions({
    queryKey: queryKeys.searchClasses(token, query),
    queryFn: ({ signal }) => factographApi.searchClasses(query, token, signal),
    staleTime: 5 * 60 * 1000
  });
}

export function searchByTypeQueryOptions(
  classId: string,
  offset: number,
  token: string
) {
  return queryOptions({
    queryKey: queryKeys.searchByType(token, classId, offset),
    queryFn: ({ signal }) => factographApi.searchByType(classId, offset, token, signal)
  });
}

export function resourcePageQueryOptions(resourceId: string, token: string) {
  return queryOptions({
    queryKey: queryKeys.resourcePage(token, resourceId),
    queryFn: ({ signal }) => factographApi.getResourcePage(resourceId, token, signal)
  });
}

export function portraitQueryOptions(resourceId: string, token: string) {
  return queryOptions({
    queryKey: queryKeys.portrait(token, resourceId),
    queryFn: ({ signal }) => factographApi.getPortrait(resourceId, token, signal)
  });
}

export function documentLocationQueryOptions(uri: string, token: string) {
  return queryOptions({
    queryKey: queryKeys.documentLocation(token, uri),
    queryFn: ({ signal }) => factographApi.getDocumentLocation(uri, token, signal)
  });
}

export function documentBlobQueryOptions(
  uri: string,
  variant: DocumentVariant,
  token: string
) {
  return queryOptions({
    queryKey: queryKeys.documentBlob(token, uri, variant),
    queryFn: ({ signal }) => factographApi.getDocumentBlob(uri, variant, token, signal),
    staleTime: Infinity,
    gcTime: 0
  });
}

export function ontologySchemaQueryOptions(token: string) {
  return queryOptions({
    queryKey: queryKeys.ontologySchema(token),
    queryFn: ({ signal }) => ontologyApi.getWriteSchema(token, signal),
    staleTime: 5 * 60 * 1000
  });
}

export function collectionContentsQueryOptions(collectionId: string, token: string) {
  return queryOptions({
    queryKey: queryKeys.collectionContents(token, collectionId),
    queryFn: ({ signal }) => collectionApi.getItems(collectionId, token, signal)
  });
}

export function adminIndexStatusQueryOptions(token: string) {
  return queryOptions({
    queryKey: queryKeys.adminIndexStatus(token),
    queryFn: ({ signal }) => adminApi.getIndexStatus(token, signal)
  });
}

export function adminPreviewStatusQueryOptions(token: string) {
  return queryOptions({
    queryKey: queryKeys.adminPreviewStatus(token),
    queryFn: ({ signal }) => adminApi.getPreviewStatus(token, signal)
  });
}

export function materializationSummaryQueryOptions(token: string) {
  return queryOptions({
    queryKey: queryKeys.materializationSummary(token),
    queryFn: ({ signal }) => adminApi.getMaterializationSummary(token, signal)
  });
}
