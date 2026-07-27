import { useEffect, useState } from "react";
import type { AuthenticationSession } from "../api/authModels";
import {
  persistAuthenticationCredentials,
  readAuthenticationCredentials
} from "../auth/authCredentials";
import { initializeAuthentication } from "../auth/authInitialization";
import { clearPendingAuthorization } from "../auth/authStorage";
import type { OidcClientConfiguration } from "../auth/oidcConfiguration";
import { beginOidcLogin } from "../auth/oidcLogin";

const initial = readAuthenticationCredentials();

function errorMessage(reason: unknown, fallback: string): string {
  return reason instanceof Error ? reason.message : fallback;
}

export function useAuthentication() {
  const [token, setToken] = useState(initial.token);
  const [session, setSession] = useState(initial.session);
  const [configuration, setConfiguration] = useState<OidcClientConfiguration | null>(null);
  const [initializing, setInitializing] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function apply(nextToken: string, nextSession: AuthenticationSession | null): void {
    persistAuthenticationCredentials(nextToken, nextSession);
    setToken(nextToken);
    setSession(nextSession);
  }

  useEffect(() => {
    let active = true;
    initializeAuthentication()
      .then(result => {
        if (!active) return;
        setConfiguration(result.configuration);
        if (result.completed !== null) {
          apply(result.completed.token, result.completed.session);
        }
      })
      .catch(reason => {
        if (active) setError(errorMessage(reason, "Не удалось выполнить вход."));
      })
      .finally(() => {
        if (active) setInitializing(false);
      });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (session?.source !== "oidc" || session.expiresAt === null) return;
    const delay = session.expiresAt - Date.now();
    if (delay <= 0) {
      apply("", null);
      setError("Сессия истекла. Войдите снова.");
      return;
    }
    const timeout = window.setTimeout(() => {
      apply("", null);
      setError("Сессия истекла. Войдите снова.");
    }, delay);
    return () => window.clearTimeout(timeout);
  }, [session]);

  async function login(): Promise<void> {
    if (configuration === null) return;
    setBusy(true);
    setError(null);
    try {
      await beginOidcLogin(configuration);
    } catch (reason) {
      setError(errorMessage(reason, "Не удалось начать вход."));
      setBusy(false);
    }
  }

  function logout(): void {
    clearPendingAuthorization();
    apply("", null);
    setError(null);
  }

  function saveDiagnosticToken(value: string): void {
    const normalized = value.trim();
    apply(normalized, normalized.length === 0
      ? null
      : { source: "diagnostic", expiresAt: null });
    setError(null);
  }

  return {
    token,
    source: session?.source ?? null,
    oidcEnabled: configuration !== null,
    initializing,
    busy,
    error,
    login,
    logout,
    saveDiagnosticToken
  };
}
