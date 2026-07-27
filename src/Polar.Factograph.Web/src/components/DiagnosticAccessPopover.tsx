import { DiagnosticTokenForm } from "./DiagnosticTokenForm";

interface DiagnosticAccessPopoverProps {
  token: string;
  source: "oidc" | "diagnostic" | null;
  error: string | null;
  onSave: (value: string) => void;
  onClose: () => void;
}

export function DiagnosticAccessPopover(props: DiagnosticAccessPopoverProps) {
  return (
    <div className="token-popover authentication-popover">
      {props.error && <div className="notice error">{props.error}</div>}
      {props.source === "oidc" ? (
        <p className="muted">
          Завершите текущую OIDC-сессию перед использованием ручного токена.
        </p>
      ) : (
        <DiagnosticTokenForm
          token={props.source === "diagnostic" ? props.token : ""}
          onSave={props.onSave}
        />
      )}
      <div className="token-actions">
        <button className="button ghost" type="button" onClick={props.onClose}>
          Закрыть
        </button>
      </div>
    </div>
  );
}
