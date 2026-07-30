import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import {
  searchByTypeQueryOptions,
  searchClassesQueryOptions
} from "./queryOptions";

export function useOntologyClassSearch(
  query: string | null,
  classId: string | null,
  offset: number,
  token: string
) {
  const normalizedQuery = query?.trim() ?? "";
  const suggestionsEnabled = query !== null &&
    classId === null &&
    normalizedQuery.length >= 2;
  const suggestions = useQuery({
    ...searchClassesQueryOptions(normalizedQuery, token),
    enabled: suggestionsEnabled
  });
  const pageEnabled = classId !== null && classId.trim().length > 0;
  const page = useQuery({
    ...searchByTypeQueryOptions(classId ?? "", offset, token),
    enabled: pageEnabled
  });

  return {
    suggestions: suggestions.data ?? [],
    suggestionsLoading: suggestions.isFetching,
    suggestionsError: suggestions.error === null ? null : errorText(suggestions.error),
    page: page.data ?? null,
    pageLoading: page.isFetching,
    pageError: page.error === null ? null : errorText(page.error),
    reload: () => {
      if (suggestionsEnabled) void suggestions.refetch();
      if (pageEnabled) void page.refetch();
    }
  };
}
