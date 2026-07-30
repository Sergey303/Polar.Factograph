import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import { ontologyValidationQueryOptions } from "./queryOptions";

export function useOntologyValidation(token: string) {
  const query = useQuery(ontologyValidationQueryOptions(token));

  return {
    report: query.data ?? null,
    loading: query.isFetching,
    error: query.error === null ? null : errorText(query.error),
    reload: () => void query.refetch()
  };
}
