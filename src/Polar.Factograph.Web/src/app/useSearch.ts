import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import type { ResourceSearchResult } from "../api/models";
import { searchQueryOptions } from "./queryOptions";

const emptyResults: ResourceSearchResult[] = [];

export function useSearch(query: string | null, token: string) {
  const normalizedQuery = query?.trim() ?? "";
  const result = useQuery({
    ...searchQueryOptions(normalizedQuery, token),
    enabled: query !== null && normalizedQuery.length > 0
  });

  return {
    query: normalizedQuery,
    results: result.data ?? emptyResults,
    loading: result.isFetching,
    error: result.error === null ? null : errorText(result.error),
    reload: () => {
      void result.refetch();
    }
  };
}
