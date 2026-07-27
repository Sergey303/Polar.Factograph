import type {
  AuthenticationSession,
  PendingAuthorization
} from "../api/authModels";

const sessionKey = "polar.factograph.auth-session";
const pendingKey = "polar.factograph.pending-authorization";

function readJson<T>(key: string): T | null {
  const raw = sessionStorage.getItem(key);
  if (raw === null) return null;
  try {
    return JSON.parse(raw) as T;
  } catch {
    sessionStorage.removeItem(key);
    return null;
  }
}

export function readAuthenticationSession(): AuthenticationSession | null {
  const value = readJson<AuthenticationSession>(sessionKey);
  if (value?.source !== "oidc" && value?.source !== "diagnostic") return null;
  return value;
}

export function writeAuthenticationSession(value: AuthenticationSession): void {
  sessionStorage.setItem(sessionKey, JSON.stringify(value));
}

export function clearAuthenticationSession(): void {
  sessionStorage.removeItem(sessionKey);
}

export function writePendingAuthorization(value: PendingAuthorization): void {
  sessionStorage.setItem(pendingKey, JSON.stringify(value));
}

export function takePendingAuthorization(): PendingAuthorization | null {
  const value = readJson<PendingAuthorization>(pendingKey);
  sessionStorage.removeItem(pendingKey);
  return value;
}

export function clearPendingAuthorization(): void {
  sessionStorage.removeItem(pendingKey);
}
