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
  callbackError: string | null;
}

let initialization: Promise<AuthenticationInitialization> | null = null;

function message(reason: unknown): string {
  return reason instanceof Error ? reason.message : "Не удалось завершить вход.";
}

async function createInitialization(): Promise<AuthenticationInitialization> {
  const publicConfiguration = await authApi.configuration();
  const configuration = enabledOidcConfiguration(publicConfiguration);
  if (!hasAuthorizationCallback()) {
    return { configuration, completed: null, callbackError: null };
  }
  if (configuration === null) {
    throw new Error("Браузерный вход не настроен на сервере.");
  }

  try {
    const result = await completeOidcLogin(configuration);
    return {
      configuration,
      completed: result === null ? null : {
        token: result.accessToken,
        session: { source: "oidc", expiresAt: result.expiresAt }
      },
      callbackError: null
    };
  } catch (reason) {
    return { configuration, completed: null, callbackError: message(reason) };
  }
}

export function initializeAuthentication(): Promise<AuthenticationInitialization> {
  initialization ??= createInitialization();
  return initialization;
}
