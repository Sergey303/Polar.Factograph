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
  standalone?: boolean;
  onLogin: (request: LocalLoginRequest) => Promise<void>;
  onRegister: (request: LocalRegisterRequest) => Promise<void>;
  onLogout: () => Promise<void>;
  onClose: () => void;
}

const loginPattern = /^[\p{L}\p{N}][\p{L}\p{N}._-]{1,61}[\p{L}\p{N}_-]$/u;

export function LocalAuthenticationPopover(
  props: LocalAuthenticationPopoverProps
) {
  const [registering, setRegistering] = useState(false);
  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [clientError, setClientError] = useState<string | null>(null);

  async function submit(event: FormEvent): Promise<void> {
    event.preventDefault();
    setClientError(null);

    const canonicalLogin = login.trim().normalize("NFKC");
    if (!loginPattern.test(canonicalLogin)) {
      setClientError(
        "Логин должен содержать от 3 до 63 букв, цифр, точек, знаков подчёркивания или дефисов, начинаться с буквы или цифры и не заканчиваться точкой."
      );
      return;
    }

    if (password.length === 0) {
      setClientError("Введите пароль.");
      return;
    }

    if (registering && password.length < 10) {
      setClientError("Пароль должен содержать не менее 10 символов.");
      return;
    }

    const deviceName = navigator.platform || "Браузер";
    if (registering) {
      await props.onRegister({
        login: canonicalLogin,
        password,
        displayName,
        deviceName
      });
    } else {
      await props.onLogin({ login: canonicalLogin, password, deviceName });
    }
  }

  const visibleError = clientError ?? props.error;

  return (
    <div className={`token-popover authentication-popover${props.standalone ? " standalone" : ""}`}>
      {visibleError && <div className="notice error">{visibleError}</div>}
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
        <form
          className="local-authentication-form"
          noValidate
          onSubmit={event => { void submit(event); }}
        >
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
              onChange={event => {
                setLogin(event.target.value);
                setClientError(null);
              }}
              autoComplete="username"
              aria-invalid={visibleError !== null}
            />
          </label>
          <label>
            Пароль
            <input
              type="password"
              value={password}
              onChange={event => {
                setPassword(event.target.value);
                setClientError(null);
              }}
              autoComplete={registering ? "new-password" : "current-password"}
              aria-invalid={visibleError !== null}
            />
          </label>
          {registering && (
            <span className="muted authentication-hint">
              Не менее 10 символов.
            </span>
          )}
          <button className="button primary" type="submit" disabled={props.busy}>
            {registering ? "Создать пользователя" : "Войти"}
          </button>
          {props.registrationEnabled && (
            <button
              className="button ghost"
              type="button"
              disabled={props.busy}
              onClick={() => {
                setRegistering(value => !value);
                setClientError(null);
              }}
            >
              {registering ? "У меня уже есть логин" : "Зарегистрироваться"}
            </button>
          )}
        </form>
      )}
      {!props.standalone && (
        <button className="button ghost" type="button" onClick={props.onClose}>
          Закрыть
        </button>
      )}
    </div>
  );
}
