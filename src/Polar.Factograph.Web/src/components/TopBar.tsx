import { useEffect, useState } from "react";
import type {
  LocalLoginRequest,
  LocalRegisterRequest,
  LocalUser
} from "../api/authModels";
import type { ProjectOverview } from "../api/models";
import { followAppLink, searchHref } from "../app/routes";
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
      <a className="brand-block" href={searchHref} onClick={followAppLink}>
        <span className="brand-mark">PF</span>
        <div>
          <strong>Polar.Factograph</strong>
          <div className="muted top-caption">
            {props.project?.name ?? "Подключение к проекту"}
          </div>
        </div>
      </a>

      <div className="top-actions">
        <a
          className="button ghost"
          href={searchHref}
          onClick={followAppLink}
        >
          Поиск
        </a>
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
