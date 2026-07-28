import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";

export function useResourcePage(
  resourceId: string | null,
  routeAddress: string | null,
  token: string
) {
  const query = useQuery({
    queryKey: ["semantic-resource-page", routeAddress, token],
    enabled: resourceId !== null && routeAddress !== null,
    queryFn: ({ signal }) => {
      if (resourceId === null) {
        throw new Error("Resource route is not selected.");
      }
      return factographApi.getResourcePage(resourceId, token, signal);
    }
  });

  return {
    page: query.data ?? null,
    loading: query.isFetching,
    error: query.error === null ? null : errorText(query.error),
    reload: () => {
      void query.refetch();
    }
  };
}
