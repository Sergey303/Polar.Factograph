import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { errorText } from "../api/errorText";
import { materializationSummaryQueryOptions } from "./queryOptions";

export function useMaterializationSummary(token: string) {
  const [requested, setRequested] = useState(false);
  const query = useQuery({
    ...materializationSummaryQueryOptions(token),
    enabled: requested
  });

  function load(): void {
    if (requested) {
      void query.refetch();
    } else {
      setRequested(true);
    }
  }

  return {
    summary: query.data ?? null,
    loading: requested && query.isFetching,
    error: query.error === null ? null : errorText(query.error),
    load
  };
}
