import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import type { ResourceSearchResult } from "../api/models";
import { searchQueryOptions } from "./queryOptions";

const emptyResults: ResourceSearchResult[] = [];

export function useSearch(query: string | null, token: string) {
  const normalizedQuery = query?.trim() ?? "";
  const enabled = query !== null && normalizedQuery.length > 0;
  const result = useQuery({
    ...searchQueryOptions(normalizedQuery, token),
    enabled
  });

  return {
    query: normalizedQuery,
    results: result.data ?? emptyResults,
    loading: result.isFetching,
    error: result.error === null ? null : errorText(result.error),
    reload: () => {
      if (enabled) void result.refetch();
    }
  };
}
