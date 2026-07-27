export interface BrowserAuthenticationConfiguration {
  enabled: boolean;
  authority: string | null;
  clientId: string | null;
  scope: string | null;
}

export interface OidcDiscoveryDocument {
  issuer: string;
  authorization_endpoint: string;
  token_endpoint: string;
}

export interface OidcTokenResponse {
  access_token: string;
  token_type?: string;
  expires_in?: number | string;
}

export interface AuthenticationSession {
  source: "oidc" | "diagnostic";
  expiresAt: number | null;
}

export interface PendingAuthorization {
  state: string;
  verifier: string;
  redirectUri: string;
  createdAt: number;
}
