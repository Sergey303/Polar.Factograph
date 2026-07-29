import { useQuery } from "@tanstack/react-query";
import { errorText } from "../api/errorText";
import { collectionContentsQueryOptions } from "./queryOptions";

export function useCollectionContents(collectionId: string | null, token: string) {
  const query = useQuery({
    ...collectionContentsQueryOptions(collectionId ?? "", token),
    enabled: collectionId !== null
  });

  return {
    contents: query.data ?? null,
    loading: collectionId !== null && query.isPending,
    error: query.error === null ? null : errorText(query.error),
    reload: () => {
      if (collectionId !== null) void query.refetch();
    }
  };
}
