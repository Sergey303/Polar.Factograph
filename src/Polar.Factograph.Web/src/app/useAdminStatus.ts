import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import {
  adminIndexStatusQueryOptions,
  adminPreviewStatusQueryOptions
} from "./queryOptions";

export function useAdminStatus(token: string, enabled: boolean) {
  const indexQuery = useQuery({
    ...adminIndexStatusQueryOptions(token),
    enabled
  });
  const previewQuery = useQuery({
    ...adminPreviewStatusQueryOptions(token),
    enabled
  });
  const errors = [indexQuery.error, previewQuery.error]
    .filter(error => error !== null)
    .map(errorText);

  return {
    index: indexQuery.data ?? null,
    previews: previewQuery.data ?? null,
    loading: enabled && (indexQuery.isPending || previewQuery.isPending),
    error: errors.length > 0 ? [...new Set(errors)].join(" · ") : null,
    reload: () => {
      if (!enabled) return;
      void indexQuery.refetch();
      void previewQuery.refetch();
    }
  };
}
