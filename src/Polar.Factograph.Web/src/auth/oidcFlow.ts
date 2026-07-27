import { authApi } from "../api/authApi";
import type { BrowserAuthenticationConfiguration } from "../api/authModels";
import {
  takePendingAuthorization,
  writePendingAuthorization
} from "./authStorage";
import { createPkce, randomUrlSafe } from "./pkce";

export interface OidcClientConfiguration {
  authority: string;
  clientId: string;
  scope: string;
}

const pendingLifetimeMs = 10 * 60 * 1000;
const callbackParameters = [
  "code",
  "state",
  "session_state",
  "iss",
  "error",
  "error_description"
];

function normalizedIssuer(value: string): string {
  return value.replace(/\/+$/, "");
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

export function hasAuthorizationCallback(): boolean {
  const parameters = new URLSearchParams(window.location.search);
  return parameters.has("code") || parameters.has("error");
}

function currentRedirectUri(): string {
  return `${window.location.origin}${window.location.pathname}`;
}

function clearCallbackUrl(url: URL): void {
  for (const name of callbackParameters) url.searchParams.delete(name);
  window.history.replaceState({}, document.title, url.toString());
}

export async function beginOidcLogin(
  configuration: OidcClientConfiguration
): Promise<void> {
  const discovery = await authApi.discover(configuration.authority);
  const pkce = await createPkce();
  const state = randomUrlSafe();
  const redirectUri = currentRedirectUri();
  writePendingAuthorization({
    state,
    verifier: pkce.verifier,
    redirectUri,
    createdAt: Date.now()
  });

  const target = new URL(discovery.authorization_endpoint);
  target.searchParams.set("response_type", "code");
  target.searchParams.set("client_id", configuration.clientId);
  target.searchParams.set("redirect_uri", redirectUri);
  target.searchParams.set("scope", configuration.scope);
  target.searchParams.set("state", state);
  target.searchParams.set("code_challenge", pkce.challenge);
  target.searchParams.set("code_challenge_method", "S256");
  window.location.assign(target.toString());
}

export async function completeOidcLogin(
  configuration: OidcClientConfiguration
): Promise<{ accessToken: string; expiresAt: number } | null> {
  const url = new URL(window.location.href);
  if (!url.searchParams.has("code") && !url.searchParams.has("error")) return null;

  const code = url.searchParams.get("code");
  const state = url.searchParams.get("state");
  const callbackIssuer = url.searchParams.get("iss");
  const providerError = url.searchParams.get("error_description")
    ?? url.searchParams.get("error");
  const pending = takePendingAuthorization();
  clearCallbackUrl(url);

  if (providerError) throw new Error(`Вход отклонён: ${providerError}`);
  if (callbackIssuer !== null &&
      normalizedIssuer(callbackIssuer) !== normalizedIssuer(configuration.authority)) {
    throw new Error("Ответ входа получен от неожиданного провайдера.");
  }
  if (!code || !state || pending === null || pending.state !== state) {
    throw new Error("Не удалось подтвердить состояние входа. Начните вход заново.");
  }
  if (!pending.verifier || !Number.isFinite(pending.createdAt) ||
      pending.redirectUri !== currentRedirectUri() ||
      Date.now() - pending.createdAt > pendingLifetimeMs ||
      pending.createdAt > Date.now() + 60_000) {
    throw new Error("Запрос на вход устарел. Начните вход заново.");
  }

  const discovery = await authApi.discover(configuration.authority);
  const token = await authApi.exchangeCode(discovery.token_endpoint, {
    grant_type: "authorization_code",
    client_id: configuration.clientId,
    code,
    redirect_uri: pending.redirectUri,
    code_verifier: pending.verifier
  });
  return {
    accessToken: token.accessToken,
    expiresAt: Date.now() + token.expiresIn * 1000
  };
}
