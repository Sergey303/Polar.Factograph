import { useEffect, useState } from "react";
import type {
  LocalLoginRequest,
  LocalRegisterRequest,
  LocalUser
} from "../api/authModels";
import type { ProjectOverview } from "../api/models";
import { LocalAuthenticationPopover } from "./LocalAuthenticationPopover";

interface TopBarAuthentication {
  authenticated: boolean;
  registrationEnabled: boolean;
  user: LocalUser | null;
  initializing: boolean;
  busy: boolean;
  error: string | null;
  onLogin: (request: LocalLoginRequest) => Promise<void>;
  onRegister: (request: LocalRegisterRequest) => Promise<void>;
  onLogout: () => Promise<void>;
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
        {props.project && <span className="user-pill">{props.project.userId}</span>}
        {props.canAdmin && (
          <button className="button" type="button" onClick={props.onAdmin}>
            Администрирование
          </button>
        )}
        <button
          className="button ghost"
          type="button"
          onClick={props.onReload}
          disabled={props.loading}
        >
          Обновить
        </button>
        <button
          className="button primary"
          type="button"
          disabled={auth.initializing || auth.busy}
          onClick={() => setExpanded(value => !value)}
        >
          {auth.user?.displayName ?? "Войти"}
        </button>
      </div>

      {expanded && (
        <LocalAuthenticationPopover
          authenticated={auth.authenticated}
          registrationEnabled={auth.registrationEnabled}
          user={auth.user}
          busy={auth.busy}
          error={auth.error}
          onLogin={auth.onLogin}
          onRegister={auth.onRegister}
          onLogout={auth.onLogout}
          onClose={() => setExpanded(false)}
        />
      )}
    </header>
  );
}
