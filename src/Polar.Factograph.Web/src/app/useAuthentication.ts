import { useEffect, useState } from "react";
import { authApi } from "../api/authApi";
import type {
  LocalLoginRequest,
  LocalRegisterRequest,
  LocalSession
} from "../api/authModels";

function errorMessage(reason: unknown, fallback: string): string {
  return reason instanceof Error ? reason.message : fallback;
}

export function useAuthentication() {
  const [session, setSession] = useState<LocalSession | null>(null);
  const [initializing, setInitializing] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    authApi.session()
      .then(value => {
        if (active) setSession(value);
      })
      .catch(reason => {
        if (active) setError(errorMessage(reason, "Не удалось проверить сессию."));
      })
      .finally(() => {
        if (active) setInitializing(false);
      });
    return () => { active = false; };
  }, []);

  async function login(request: LocalLoginRequest): Promise<void> {
    setBusy(true);
    setError(null);
    try {
      setSession(await authApi.login(request));
    } catch (reason) {
      setError(errorMessage(reason, "Не удалось войти."));
    } finally {
      setBusy(false);
    }
  }

  async function register(request: LocalRegisterRequest): Promise<void> {
    setBusy(true);
    setError(null);
    try {
      setSession(await authApi.register(request));
    } catch (reason) {
      setError(errorMessage(reason, "Не удалось зарегистрироваться."));
    } finally {
      setBusy(false);
    }
  }

  async function logout(): Promise<void> {
    setBusy(true);
    setError(null);
    try {
      setSession(await authApi.logout());
    } catch (reason) {
      setError(errorMessage(reason, "Не удалось выйти."));
    } finally {
      setBusy(false);
    }
  }

  const authenticated = session?.authenticated === true && session.user !== null;
  return {
    token: authenticated ? session.user!.id : "",
    authenticated,
    registrationEnabled: session?.registrationEnabled ?? false,
    user: session?.user ?? null,
    devices: session?.devices ?? [],
    initializing,
    busy,
    error,
    login,
    register,
    logout
  };
}
