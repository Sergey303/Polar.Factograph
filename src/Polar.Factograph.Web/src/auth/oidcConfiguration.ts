import type { BrowserAuthenticationConfiguration } from "../api/authModels";

export interface OidcClientConfiguration {
  authority: string;
  clientId: string;
  scope: string;
}

export function enabledOidcConfiguration(
  value: BrowserAuthenticationConfiguration
): OidcClientConfiguration | null {
  if (!value.enabled) return null;
  if (!value.authority || !value.clientId || !value.scope) {
    throw new Error("API вернул неполную конфигурацию браузерного входа.");
  }
  return {
    authority: value.authority,
    clientId: value.clientId,
    scope: value.scope
  };
}
