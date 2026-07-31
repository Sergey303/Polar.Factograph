import { useState } from "react";
import { adminApi } from "../api/adminApi";
import type { ProjectIndexRebuildResult } from "../api/adminModels";
import { errorText } from "../api/errorText";

export function useIndexRebuild(token: string, onCompleted: () => void) {
  const [result, setResult] = useState<ProjectIndexRebuildResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function rebuild(): Promise<void> {
    const confirmed = window.confirm(
      "Перечитать список кассет, проверить FOG редакторов и полностью пересоздать " +
      "опорную последовательность и поисковые индексы Polar.DB?"
    );
    if (!confirmed) return;

    setBusy(true);
    setError(null);
    setResult(null);
    try {
      const next = await adminApi.rebuildIndex(token);
      setResult(next);
    } catch (reason) {
      setError(errorText(reason));
    } finally {
      onCompleted();
      setBusy(false);
    }
  }

  return { result, busy, error, rebuild };
}
