import { useCallback, useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { ProjectOverview } from "../api/models";

export function useProject(token: string) {
  const [project, setProject] = useState<ProjectOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);

  const reload = useCallback(() => setRevision(value => value + 1), []);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);

    factographApi.getProject(token, controller.signal)
      .then(setProject)
      .catch(reason => {
        if (!controller.signal.aborted) {
          setProject(null);
          setError(errorText(reason));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });

    return () => controller.abort();
  }, [token, revision]);

  return { project, loading, error, reload };
}
