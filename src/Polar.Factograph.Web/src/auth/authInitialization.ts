import { authApi } from "../api/authApi";
import type { AuthenticationSession } from "../api/authModels";
import { hasAuthorizationCallback } from "./browserAuthorizationLocation";
import { completeOidcLogin } from "./oidcCallback";
import {
  enabledOidcConfiguration,
  type OidcClientConfiguration
} from "./oidcConfiguration";

export interface AuthenticationInitialization {
  configuration: OidcClientConfiguration | null;
  completed: {
    token: string;
    session: AuthenticationSession;
  } | null;
}

export async function initializeAuthentication(
  signal: AbortSignal
): Promise<AuthenticationInitialization> {
  const publicConfiguration = await authApi.configuration(signal);
  signal.throwIfAborted();
  const configuration = enabledOidcConfiguration(publicConfiguration);
  if (!hasAuthorizationCallback()) return { configuration, completed: null };
  if (configuration === null) {
    throw new Error("Браузерный вход не настроен на сервере.");
  }

  const result = await completeOidcLogin(configuration);
  signal.throwIfAborted();
  return {
    configuration,
    completed: result === null ? null : {
      token: result.accessToken,
      session: { source: "oidc", expiresAt: result.expiresAt }
    }
  };
}
