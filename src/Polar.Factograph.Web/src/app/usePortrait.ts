import { useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { ResourcePortrait } from "../api/models";

export function usePortrait(resourceId: string | null, token: string) {
  const [portrait, setPortrait] = useState<ResourcePortrait | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (resourceId === null) {
      setPortrait(null);
      setError(null);
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    setLoading(true);
    setError(null);
    factographApi.getPortrait(resourceId, token, controller.signal)
      .then(setPortrait)
      .catch(reason => {
        if (!controller.signal.aborted) {
          setPortrait(null);
          setError(errorText(reason));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });

    return () => controller.abort();
  }, [resourceId, token]);

  return { portrait, loading, error };
}
