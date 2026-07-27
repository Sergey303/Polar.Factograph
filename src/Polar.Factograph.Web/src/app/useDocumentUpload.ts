import { useState } from "react";
import { documentWriteApi } from "../api/documentWriteApi";
import type { DocumentWriteResponse } from "../api/documentWriteModels";
import { errorText } from "../api/errorText";

export function useDocumentUpload(
  token: string,
  onUploaded: (result: DocumentWriteResponse) => void
) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function upload(file: File | null, cassetteId: string): Promise<void> {
    if (file === null) {
      setError("Выберите файл документа.");
      return;
    }
    if (cassetteId.trim().length === 0) {
      setError("Выберите кассету документа.");
      return;
    }

    setBusy(true);
    setError(null);
    try {
      onUploaded(await documentWriteApi.add(file, cassetteId, token));
    } catch (reason) {
      setError(errorText(reason));
    } finally {
      setBusy(false);
    }
  }

  return { busy, error, upload };
}
