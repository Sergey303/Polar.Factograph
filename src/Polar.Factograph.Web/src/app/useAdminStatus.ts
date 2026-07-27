import { useCallback, useEffect, useState } from "react";
import { adminApi } from "../api/adminApi";
import type {
  PreviewSubsystemStatus,
  ProjectIndexRuntimeStatus
} from "../api/adminModels";
import { errorText } from "../api/errorText";

export function useAdminStatus(token: string, enabled: boolean) {
  const [index, setIndex] = useState<ProjectIndexRuntimeStatus | null>(null);
  const [previews, setPreviews] = useState<PreviewSubsystemStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);

  const reload = useCallback(() => setRevision(value => value + 1), []);

  useEffect(() => {
    if (!enabled) return;
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    Promise.all([
      adminApi.getIndexStatus(token, controller.signal),
      adminApi.getPreviewStatus(token, controller.signal)
    ])
      .then(([nextIndex, nextPreviews]) => {
        setIndex(nextIndex);
        setPreviews(nextPreviews);
      })
      .catch(reason => {
        if (!controller.signal.aborted) setError(errorText(reason));
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [token, enabled, revision]);

  return { index, previews, loading, error, reload };
}
