import { useState } from "react";
import { documentWriteApi } from "../api/documentWriteApi";
import { errorText } from "../api/errorText";

interface DocumentReplaceControlProps {
  uri: string;
  token: string;
  enabled: boolean;
  onReplaced: () => void;
}

export function DocumentReplaceControl({
  uri,
  token,
  enabled,
  onReplaced
}: DocumentReplaceControlProps) {
  const [file, setFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  async function replace(): Promise<void> {
    if (file === null) return;
    setBusy(true);
    setMessage(null);
    try {
      const result = await documentWriteApi.replace(uri, file, token);
      setMessage(result.previewState === "queued" ? "Файл заменён, превью поставлено в очередь." : "Файл заменён.");
      setFile(null);
      onReplaced();
    } catch (reason) {
      setMessage(errorText(reason));
    } finally {
      setBusy(false);
    }
  }

  if (!enabled) {
    return <span className="muted">Замена недоступна.</span>;
  }

  return (
    <div className="document-replace-control">
      <input type="file" onChange={event => setFile(event.target.files?.[0] ?? null)} />
      <button className="button subtle compact" disabled={busy || file === null} onClick={() => void replace()}>
        Заменить оригинал
      </button>
      {message && <span className="muted">{message}</span>}
    </div>
  );
}
