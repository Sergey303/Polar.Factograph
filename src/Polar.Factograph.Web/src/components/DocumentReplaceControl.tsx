import { useRef, useState, type ChangeEvent } from "react";
import { documentWriteApi } from "../api/documentWriteApi";
import { errorText } from "../api/errorText";
import { UiIcon } from "./UiIcon";

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
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  async function replace(file: File): Promise<void> {
    const confirmed = window.confirm(
      `Заменить изображение файлом «${file.name}»? Адрес сущности останется прежним.`
    );
    if (!confirmed) return;

    setBusy(true);
    setMessage(null);
    try {
      const result = await documentWriteApi.replace(uri, file, token);
      setMessage(result.previewState === "queued"
        ? "Изображение заменено. Подготавливаются размеры для просмотра."
        : "Изображение заменено.");
      onReplaced();
    } catch (reason) {
      setMessage(errorText(reason));
    } finally {
      setBusy(false);
    }
  }

  function chooseReplacement(): void {
    const input = inputRef.current;
    if (input === null) return;

    setMessage(null);
    input.value = "";
    input.click();
  }

  function selectedReplacement(event: ChangeEvent<HTMLInputElement>): void {
    const input = event.currentTarget;
    const selected = input.files?.[0] ?? null;
    input.value = "";
    if (selected !== null) void replace(selected);
  }

  if (!enabled) return null;

  return (
    <div className="document-replace-control">
      <input
        ref={inputRef}
        type="file"
        hidden
        accept="image/*"
        aria-label="Выбрать новое изображение"
        onChange={selectedReplacement}
      />
      <button
        type="button"
        className="button subtle compact"
        disabled={busy}
        onClick={chooseReplacement}
      >
        <UiIcon name="replace" spinning={busy} />
        <span>{busy ? "Замена…" : "Заменить изображение…"}</span>
      </button>
      {message && <span className="muted">{message}</span>}
    </div>
  );
}
