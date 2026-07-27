import {
  requestEmpty,
  requestJson,
  requestJsonBody,
  setAntiforgeryToken
} from "./http";
import type {
  LocalAuthenticatedResponse,
  LocalLoginRequest,
  LocalRegisterRequest,
  LocalSession
} from "./authModels";

async function refreshSession(signal?: AbortSignal): Promise<LocalSession> {
  const session = await requestJson<LocalSession>("api/auth/session", "", signal);
  setAntiforgeryToken(session.antiforgeryToken);
  return session;
}

export const authApi = {
  session: refreshSession,

  async login(request: LocalLoginRequest): Promise<LocalSession> {
    await requestJsonBody<LocalAuthenticatedResponse>(
      "api/auth/login",
      "POST",
      request,
      ""
    );
    return refreshSession();
  },

  async register(request: LocalRegisterRequest): Promise<LocalSession> {
    await requestJsonBody<LocalAuthenticatedResponse>(
      "api/auth/register",
      "POST",
      request,
      ""
    );
    return refreshSession();
  },

  async logout(): Promise<LocalSession> {
    await requestEmpty("api/auth/logout", "POST", "");
    return refreshSession();
  }
};
