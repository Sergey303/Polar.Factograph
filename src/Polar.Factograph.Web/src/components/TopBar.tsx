import { useEffect, useState } from "react";
import type { ProjectOverview } from "../api/models";
import { DiagnosticTokenForm } from "./DiagnosticTokenForm";
import { OidcSessionControls } from "./OidcSessionControls";

interface TopBarAuthentication {
  token: string;
  source: "oidc" | "diagnostic" | null;
  oidcEnabled: boolean;
  initializing: boolean;
  busy: boolean;
  error: string | null;
  onLogin: () => void;
  onLogout: () => void;
  onDiagnosticToken: (value: string) => void;
}

interface TopBarProps {
  project: ProjectOverview | null;
  loading: boolean;
  canAdmin: boolean;
  authentication: TopBarAuthentication;
  onReload: () => void;
  onAdmin: () => void;
}

export function TopBar(props: TopBarProps) {
  const [expanded, setExpanded] = useState(false);
  const auth = props.authentication;

  useEffect(() => {
    if (auth.error !== null) setExpanded(true);
  }, [auth.error]);

  function saveDiagnostic(value: string): void {
    auth.onDiagnosticToken(value);
    setExpanded(false);
  }

  return (
    <header className="top-bar">
      <div className="brand-block">
        <span className="brand-mark">PF</span>
        <div>
          <strong>Polar.Factograph</strong>
          <div className="muted top-caption">
            {props.project?.name ?? "Подключение к проекту"}
          </div>
        </div>
      </div>

      <div className="top-actions">
        {props.project && (
          <span className="user-pill" title={props.project.userId}>
            {props.project.userId}
          </span>
        )}
        {props.canAdmin && (
          <button className="button" type="button" onClick={props.onAdmin}>
            Администрирование
          </button>
        )}
        <button className="button ghost" type="button" onClick={props.onReload} disabled={props.loading}>
          Обновить
        </button>
        <OidcSessionControls
          authenticated={auth.source === "oidc"}
          enabled={auth.oidcEnabled}
          initializing={auth.initializing}
          busy={auth.busy}
          onLogin={auth.onLogin}
          onLogout={auth.onLogout}
        />
        <button className="button" type="button" onClick={() => setExpanded(value => !value)}>
          Диагностика
        </button>
      </div>

      {expanded && (
        <div className="token-popover authentication-popover">
          {auth.error && <div className="notice error">{auth.error}</div>}
          {auth.source === "oidc" ? (
            <p className="muted">
              Завершите текущую OIDC-сессию перед использованием ручного токена.
            </p>
          ) : (
            <DiagnosticTokenForm
              token={auth.source === "diagnostic" ? auth.token : ""}
              onSave={saveDiagnostic}
            />
          )}
          <div className="token-actions">
            <button className="button ghost" type="button" onClick={() => setExpanded(false)}>
              Закрыть
            </button>
          </div>
        </div>
      )}
    </header>
  );
}
