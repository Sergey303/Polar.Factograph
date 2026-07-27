import type { AuthenticationSession } from "../api/authModels";
import { readAccessToken, writeAccessToken } from "../api/tokenStore";
import {
  clearAuthenticationSession,
  readAuthenticationSession,
  writeAuthenticationSession
} from "./authStorage";

export interface AuthenticationCredentials {
  token: string;
  session: AuthenticationSession | null;
}

export function readAuthenticationCredentials(): AuthenticationCredentials {
  const token = readAccessToken();
  const stored = readAuthenticationSession();
  if (token.length === 0) return { token: "", session: null };
  if (stored?.source === "oidc" &&
      stored.expiresAt !== null &&
      stored.expiresAt <= Date.now()) {
    persistAuthenticationCredentials("", null);
    return { token: "", session: null };
  }
  return {
    token,
    session: stored ?? { source: "diagnostic", expiresAt: null }
  };
}

export function persistAuthenticationCredentials(
  token: string,
  session: AuthenticationSession | null
): void {
  writeAccessToken(token);
  if (session === null) clearAuthenticationSession();
  else writeAuthenticationSession(session);
}
