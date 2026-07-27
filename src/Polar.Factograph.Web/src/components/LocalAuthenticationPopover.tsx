import { useState, type FormEvent } from "react";
import type {
  LocalLoginRequest,
  LocalRegisterRequest,
  LocalUser
} from "../api/authModels";

interface LocalAuthenticationPopoverProps {
  authenticated: boolean;
  registrationEnabled: boolean;
  user: LocalUser | null;
  busy: boolean;
  error: string | null;
  onLogin: (request: LocalLoginRequest) => Promise<void>;
  onRegister: (request: LocalRegisterRequest) => Promise<void>;
  onLogout: () => Promise<void>;
  onClose: () => void;
}

export function LocalAuthenticationPopover(
  props: LocalAuthenticationPopoverProps
) {
  const [registering, setRegistering] = useState(false);
  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");

  async function submit(event: FormEvent): Promise<void> {
    event.preventDefault();
    const deviceName = navigator.userAgentData?.platform
      ?? navigator.platform
      ?? "Browser";
    if (registering) {
      await props.onRegister({ login, password, displayName, deviceName });
    } else {
      await props.onLogin({ login, password, deviceName });
    }
  }

  return (
    <div className="token-popover authentication-popover">
      {props.error && <div className="notice error">{props.error}</div>}
      {props.authenticated && props.user ? (
        <>
          <strong>{props.user.displayName}</strong>
          <span className="muted">{props.user.login}</span>
          <button
            className="button"
            type="button"
            disabled={props.busy}
            onClick={() => { void props.onLogout(); }}
          >
            Выйти
          </button>
        </>
      ) : (
        <form className="local-authentication-form" onSubmit={event => { void submit(event); }}>
          <strong>{registering ? "Регистрация" : "Вход"}</strong>
          {registering && (
            <label>
              Отображаемое имя
              <input
                value={displayName}
                onChange={event => setDisplayName(event.target.value)}
                autoComplete="name"
              />
            </label>
          )}
          <label>
            Логин
            <input
              value={login}
              onChange={event => setLogin(event.target.value)}
              autoComplete="username"
              required
            />
          </label>
          <label>
            Пароль
            <input
              type="password"
              value={password}
              onChange={event => setPassword(event.target.value)}
              autoComplete={registering ? "new-password" : "current-password"}
              minLength={10}
              required
            />
          </label>
          <button className="button primary" type="submit" disabled={props.busy}>
            {registering ? "Создать пользователя" : "Войти"}
          </button>
          {props.registrationEnabled && (
            <button
              className="button ghost"
              type="button"
              disabled={props.busy}
              onClick={() => setRegistering(value => !value)}
            >
              {registering ? "У меня уже есть логин" : "Зарегистрироваться"}
            </button>
          )}
        </form>
      )}
      <button className="button ghost" type="button" onClick={props.onClose}>
        Закрыть
      </button>
    </div>
  );
}
