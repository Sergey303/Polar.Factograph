interface OidcSessionControlsProps {
  authenticated: boolean;
  enabled: boolean;
  initializing: boolean;
  busy: boolean;
  onLogin: () => void;
  onLogout: () => void;
}

export function OidcSessionControls(props: OidcSessionControlsProps) {
  if (props.authenticated) {
    return (
      <button className="button subtle" type="button" onClick={props.onLogout}>
        Выйти
      </button>
    );
  }

  if (!props.enabled) return null;

  return (
    <button
      className="button primary"
      type="button"
      disabled={props.initializing || props.busy}
      onClick={props.onLogin}
    >
      {props.busy ? "Переход…" : "Войти"}
    </button>
  );
}
