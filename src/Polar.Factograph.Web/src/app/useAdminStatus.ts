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
    Promise.allSettled([
      adminApi.getIndexStatus(token, controller.signal),
      adminApi.getPreviewStatus(token, controller.signal)
    ]).then(([indexResult, previewResult]) => {
      if (controller.signal.aborted) return;
      if (indexResult.status === "fulfilled") setIndex(indexResult.value);
      if (previewResult.status === "fulfilled") setPreviews(previewResult.value);
      const errors = [indexResult, previewResult]
        .filter(result => result.status === "rejected")
        .map(result => errorText(result.reason));
      setError(errors.length > 0 ? errors.join(" · ") : null);
    }).finally(() => {
      if (!controller.signal.aborted) setLoading(false);
    });
    return () => controller.abort();
  }, [token, enabled, revision]);

  return { index, previews, loading, error, reload };
}
