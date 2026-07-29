import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import { portraitQueryOptions } from "./queryOptions";

export function usePortrait(resourceId: string | null, token: string) {
  const query = useQuery({
    ...portraitQueryOptions(resourceId ?? "", token),
    enabled: resourceId !== null
  });

  return {
    portrait: query.data ?? null,
    loading: resourceId !== null && query.isPending,
    error: query.error === null ? null : errorText(query.error),
    reload: () => {
      void query.refetch();
    }
  };
}
