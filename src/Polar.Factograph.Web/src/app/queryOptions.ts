import { queryOptions } from "@tanstack/react-query";
import { factographApi } from "../api/factographApi";
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

export function ontologySchemaQueryOptions(token: string) {
  return queryOptions({
    queryKey: ["ontology-write-schema", token] as const,
    queryFn: ({ signal }) => ontologyApi.getWriteSchema(token, signal),
    staleTime: 5 * 60 * 1000
  });
}
