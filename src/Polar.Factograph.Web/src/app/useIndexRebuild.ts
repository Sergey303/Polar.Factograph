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
      "Полностью перестроить индекс проекта? Чтение может быть временно недоступно."
    );
    if (!confirmed) return;

    setBusy(true);
    setError(null);
    try {
      const next = await adminApi.rebuildIndex(token);
      setResult(next);
      onCompleted();
    } catch (reason) {
      setError(errorText(reason));
    } finally {
      setBusy(false);
    }
  }

  return { result, busy, error, rebuild };
}
