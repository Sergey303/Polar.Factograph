import { useEffect, useState } from "react";

interface DiagnosticTokenFormProps {
  token: string;
  onSave: (value: string) => void;
}

export function DiagnosticTokenForm(props: DiagnosticTokenFormProps) {
  const [draft, setDraft] = useState(props.token);

  useEffect(() => setDraft(props.token), [props.token]);

  return (
    <details className="diagnostic-access">
      <summary>Диагностический JWT</summary>
      <p className="muted">
        Только для разработки и проверки внешнего провайдера. Токен хранится до закрытия вкладки.
      </p>
      <label htmlFor="access-token">Bearer-токен</label>
      <textarea
        id="access-token"
        value={draft}
        onChange={event => setDraft(event.target.value)}
        rows={4}
        placeholder="В режиме разработки поле можно оставить пустым"
      />
      <div className="token-actions">
        <button
          className="button ghost"
          type="button"
          onClick={() => {
            setDraft("");
            props.onSave("");
          }}
        >
          Очистить
        </button>
        <button
          className="button primary"
          type="button"
          onClick={() => props.onSave(draft)}
        >
          Применить
        </button>
      </div>
    </details>
  );
}
