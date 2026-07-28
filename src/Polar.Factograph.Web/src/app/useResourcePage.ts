import { useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { SemanticResourcePage } from "../api/models";

interface LoadedResourcePage {
  requestedResourceId: string;
  page: SemanticResourcePage;
}

export function useResourcePage(resourceId: string | null, token: string) {
  const [loaded, setLoaded] = useState<LoadedResourcePage | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    if (resourceId === null) {
      setLoaded(null);
      setError(null);
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    setLoaded(null);
    setLoading(true);
    setError(null);
    factographApi.getResourcePage(resourceId, token, controller.signal)
      .then(page => {
        if (!controller.signal.aborted) {
          setLoaded({ requestedResourceId: resourceId, page });
        }
      })
      .catch(reason => {
        if (!controller.signal.aborted) {
          setLoaded(null);
          setError(errorText(reason));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [resourceId, token, revision]);

  return {
    page: loaded?.page ?? null,
    loadedResourceId: loaded?.requestedResourceId ?? null,
    loading,
    error,
    reload: () => setRevision(value => value + 1)
  };
}
