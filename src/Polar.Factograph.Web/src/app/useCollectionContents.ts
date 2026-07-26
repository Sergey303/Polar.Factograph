import { useCallback, useEffect, useState } from "react";
import { collectionApi } from "../api/collectionApi";
import type { CollectionContents } from "../api/collectionModels";
import { errorText } from "../api/errorText";

export function useCollectionContents(collectionId: string | null, token: string) {
  const [contents, setContents] = useState<CollectionContents | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);

  const reload = useCallback(() => setRevision(value => value + 1), []);

  useEffect(() => {
    if (collectionId === null) {
      setContents(null);
      setError(null);
      return;
    }

    const controller = new AbortController();
    setLoading(true);
    setError(null);
    collectionApi
      .getItems(collectionId, token, controller.signal)
      .then(setContents)
      .catch(reason => {
        if (!controller.signal.aborted) {
          setContents(null);
          setError(errorText(reason));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });

    return () => controller.abort();
  }, [collectionId, token, revision]);

  return { contents, loading, error, reload };
}
