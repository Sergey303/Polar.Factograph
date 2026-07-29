import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import { projectQueryOptions } from "./queryOptions";

export function useProject(token: string) {
  const query = useQuery(projectQueryOptions(token));

  return {
    project: query.data ?? null,
    loading: query.isFetching,
    error: query.error === null ? null : errorText(query.error),
    reload: () => {
      void query.refetch();
    }
  };
}
