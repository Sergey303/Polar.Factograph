import { useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import { ontologyApi } from "../api/ontologyApi";
import type { OntologyWriteSchema } from "../api/ontologyModels";

export function useOntologySchema(token: string, enabled: boolean) {
  const [schema, setSchema] = useState<OntologyWriteSchema | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!enabled) {
      setSchema(null);
      setError(null);
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    setLoading(true);
    setError(null);
    ontologyApi.getWriteSchema(token, controller.signal)
      .then(setSchema)
      .catch(reason => {
        if (!controller.signal.aborted) {
          setSchema(null);
          setError(errorText(reason));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [token, enabled]);

  return { schema, loading, error };
}