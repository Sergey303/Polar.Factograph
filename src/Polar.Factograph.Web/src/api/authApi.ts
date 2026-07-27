import { requestJson } from "./http";
import type {
  BrowserAuthenticationConfiguration,
  OidcDiscoveryDocument,
  OidcTokenResponse
} from "./authModels";

function normalizedIssuer(value: string): string {
  return value.replace(/\/+$/, "");
}

function httpEndpoint(value: string, name: string): string {
  const url = new URL(value);
  if (url.protocol !== "https:" && url.protocol !== "http:") {
    throw new Error(`${name} должен использовать HTTP или HTTPS.`);
  }
  return url.toString();
}

async function responseJson(response: Response): Promise<Record<string, unknown>> {
  try {
    return await response.json() as Record<string, unknown>;
  } catch {
    return {};
  }
}

export const authApi = {
  configuration(signal?: AbortSignal): Promise<BrowserAuthenticationConfiguration> {
    return requestJson<BrowserAuthenticationConfiguration>("/api/auth/browser", "", signal);
  },

  async discover(authority: string): Promise<OidcDiscoveryDocument> {
    const response = await fetch(
      `${normalizedIssuer(authority)}/.well-known/openid-configuration`,
      { headers: { Accept: "application/json" } }
    );
    const value = await responseJson(response) as Partial<OidcDiscoveryDocument>;
    if (!response.ok) throw new Error(`Не удалось загрузить OpenID-конфигурацию: HTTP ${response.status}.`);
    if (normalizedIssuer(value.issuer ?? "") !== normalizedIssuer(authority)) {
      throw new Error("Провайдер вернул неожиданный issuer.");
    }
    return {
      issuer: value.issuer ?? authority,
      authorization_endpoint: httpEndpoint(value.authorization_endpoint ?? "", "authorization_endpoint"),
      token_endpoint: httpEndpoint(value.token_endpoint ?? "", "token_endpoint")
    };
  },

  async exchangeCode(
    endpoint: string,
    values: Record<string, string>
  ): Promise<{ accessToken: string; expiresIn: number }> {
    const response = await fetch(endpoint, {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/x-www-form-urlencoded"
      },
      body: new URLSearchParams(values)
    });
    const value = await responseJson(response) as Partial<OidcTokenResponse> & {
      error_description?: string;
    };
    if (!response.ok) {
      throw new Error(value.error_description ?? `Провайдер отклонил код: HTTP ${response.status}.`);
    }
    const expiresIn = Number(value.expires_in);
    if (!value.access_token || !Number.isFinite(expiresIn) || expiresIn <= 0) {
      throw new Error("Провайдер вернул неполный ответ с токеном.");
    }
    if (value.token_type && value.token_type.toLowerCase() !== "bearer") {
      throw new Error("Провайдер вернул неподдерживаемый тип токена.");
    }
    return { accessToken: value.access_token, expiresIn };
  }
};
