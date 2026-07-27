import { authApi } from "../api/authApi";
import { writePendingAuthorization } from "./authStorage";
import { currentRedirectUri } from "./browserAuthorizationLocation";
import type { OidcClientConfiguration } from "./oidcConfiguration";
import { createPkce, randomUrlSafe } from "./pkce";

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
