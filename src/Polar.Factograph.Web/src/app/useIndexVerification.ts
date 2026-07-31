import { useState } from "react";
import { adminApi } from "../api/adminApi";
import type { ProjectIndexVerificationReport } from "../api/adminModels";
import { errorText } from "../api/errorText";

export function useIndexVerification(token: string) {
  const [result, setResult] = useState<ProjectIndexVerificationReport | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function verify(): Promise<void> {
    const confirmed = window.confirm(
      "Перечитать текущие FOG, сравнить все ресурсы и тройки с активным поколением " +
      "Polar.DB и сохранить JSON-отчёт? Проверка может потребовать много памяти."
    );
    if (!confirmed) return;

    setBusy(true);
    setError(null);
    setResult(null);
    try {
      setResult(await adminApi.verifyIndex(token));
    } catch (reason) {
      setError(errorText(reason));
    } finally {
      setBusy(false);
    }
  }

  return { result, busy, error, verify };
}
