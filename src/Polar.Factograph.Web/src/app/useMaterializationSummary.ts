import { useEffect, useState } from "react";
import { adminApi } from "../api/adminApi";
import type { FogMaterializationStatistics } from "../api/adminModels";
import { errorText } from "../api/errorText";

export function useMaterializationSummary(token: string) {
  const [summary, setSummary] = useState<FogMaterializationStatistics | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [controller, setController] = useState<AbortController | null>(null);

  useEffect(() => () => controller?.abort(), [controller]);

  async function load(): Promise<void> {
    controller?.abort();
    const next = new AbortController();
    setController(next);
    setLoading(true);
    setError(null);
    try {
      setSummary(await adminApi.getMaterializationSummary(token, next.signal));
    } catch (reason) {
      if (!next.signal.aborted) setError(errorText(reason));
    } finally {
      if (!next.signal.aborted) setLoading(false);
    }
  }

  return { summary, loading, error, load };
}
