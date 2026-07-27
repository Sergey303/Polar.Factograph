import { authApi } from "../api/authApi";
import { takePendingAuthorization } from "./authStorage";
import {
  clearAuthorizationCallback,
  currentRedirectUri,
  normalizedIssuer
} from "./browserAuthorizationLocation";
import type { OidcClientConfiguration } from "./oidcConfiguration";

const pendingLifetimeMs = 10 * 60 * 1000;

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
  clearAuthorizationCallback(url);

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
