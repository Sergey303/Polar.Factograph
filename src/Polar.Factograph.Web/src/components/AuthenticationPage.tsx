import type {
  LocalLoginRequest,
  LocalRegisterRequest,
  LocalUser
} from "../api/authModels";
import { LocalAuthenticationPopover } from "./LocalAuthenticationPopover";

interface AuthenticationPageProps {
  initializing: boolean;
  registrationEnabled: boolean;
  user: LocalUser | null;
  busy: boolean;
  error: string | null;
  onLogin: (request: LocalLoginRequest) => Promise<void>;
  onRegister: (request: LocalRegisterRequest) => Promise<void>;
  onLogout: () => Promise<void>;
}

export function AuthenticationPage(props: AuthenticationPageProps) {
  return (
    <main className="authentication-page">
      <section className="authentication-card">
        <div className="authentication-brand">
          <span className="brand-mark">PF</span>
          <div>
            <strong>Polar.Factograph</strong>
            <div className="muted">Вход в проект</div>
          </div>
        </div>

        {props.initializing ? (
          <div className="authentication-loading">Проверяем сессию…</div>
        ) : (
          <LocalAuthenticationPopover
            authenticated={false}
            registrationEnabled={props.registrationEnabled}
            user={props.user}
            busy={props.busy}
            error={props.error}
            standalone
            onLogin={props.onLogin}
            onRegister={props.onRegister}
            onLogout={props.onLogout}
            onClose={() => undefined}
          />
        )}
      </section>
    </main>
  );
}
