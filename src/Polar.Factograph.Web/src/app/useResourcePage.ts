import { useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { SemanticResourcePage } from "../api/models";

export function useResourcePage(resourceId: string | null, token: string) {
  const [page, setPage] = useState<SemanticResourcePage | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    if (resourceId === null) {
      setPage(null);
      setError(null);
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    setLoading(true);
    setError(null);
    factographApi.getResourcePage(resourceId, token, controller.signal)
      .then(setPage)
      .catch(reason => {
        if (!controller.signal.aborted) {
          setPage(null);
          setError(errorText(reason));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [resourceId, token, revision]);

  return {
    page,
    loading,
    error,
    reload: () => setRevision(value => value + 1)
  };
}
