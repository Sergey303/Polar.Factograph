import { useEffect, useState } from "react";
import { authApi } from "../api/authApi";
import type { AuthenticationSession } from "../api/authModels";
import { readAccessToken, writeAccessToken } from "../api/tokenStore";
import {
  clearAuthenticationSession,
  clearPendingAuthorization,
  readAuthenticationSession,
  writeAuthenticationSession
} from "../auth/authStorage";
import {
  beginOidcLogin,
  completeOidcLogin,
  enabledOidcConfiguration,
  hasAuthorizationCallback,
  type OidcClientConfiguration
} from "../auth/oidcFlow";

function initialCredentials(): {
  token: string;
  session: AuthenticationSession | null;
} {
  const token = readAccessToken();
  const stored = readAuthenticationSession();
  if (token.length === 0) return { token: "", session: null };
  if (stored?.source === "oidc" && stored.expiresAt !== null && stored.expiresAt <= Date.now()) {
    writeAccessToken("");
    clearAuthenticationSession();
    return { token: "", session: null };
  }
  return {
    token,
    session: stored ?? { source: "diagnostic", expiresAt: null }
  };
}

const initial = initialCredentials();

export function useAuthentication() {
  const [token, setToken] = useState(initial.token);
  const [session, setSession] = useState(initial.session);
  const [configuration, setConfiguration] = useState<OidcClientConfiguration | null>(null);
  const [initializing, setInitializing] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function apply(nextToken: string, nextSession: AuthenticationSession | null): void {
    writeAccessToken(nextToken);
    if (nextSession === null) clearAuthenticationSession();
    else writeAuthenticationSession(nextSession);
    setToken(nextToken);
    setSession(nextSession);
  }

  useEffect(() => {
    const controller = new AbortController();
    authApi.configuration(controller.signal)
      .then(async value => {
        const configured = enabledOidcConfiguration(value);
        setConfiguration(configured);
        if (!hasAuthorizationCallback()) return;
        if (configured === null) throw new Error("Браузерный вход не настроен на сервере.");
        const completed = await completeOidcLogin(configured);
        if (completed !== null) {
          apply(completed.accessToken, {
            source: "oidc",
            expiresAt: completed.expiresAt
          });
        }
      })
      .catch(reason => {
        if (!controller.signal.aborted) {
          setError(reason instanceof Error ? reason.message : "Не удалось выполнить вход.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setInitializing(false);
      });
    return () => controller.abort();
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
      setError(reason instanceof Error ? reason.message : "Не удалось начать вход.");
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
