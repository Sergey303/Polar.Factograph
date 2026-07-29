import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import { resourcePageQueryOptions } from "./queryOptions";

export function useResourcePage(resourceId: string | null, token: string) {
  const query = useQuery({
    ...resourcePageQueryOptions(resourceId ?? "", token),
    enabled: resourceId !== null
  });

  return {
    page: query.data ?? null,
    loading: resourceId !== null && query.isPending,
    refreshing: query.isFetching,
    error: query.error === null ? null : errorText(query.error),
    reload: () => {
      void query.refetch();
    }
  };
}
