import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import { ontologySchemaQueryOptions } from "./queryOptions";

export function useOntologySchema(token: string, enabled: boolean) {
  const query = useQuery({
    ...ontologySchemaQueryOptions(token),
    enabled
  });

  return {
    schema: query.data ?? null,
    loading: enabled && query.isPending,
    error: query.error === null ? null : errorText(query.error),
    reload: () => {
      void query.refetch();
    }
  };
}
