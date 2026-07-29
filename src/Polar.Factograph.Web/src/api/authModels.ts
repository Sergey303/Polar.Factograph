export interface LocalUser {
  id: string;
  login: string;
  displayName: string;
  roles: string[];
  fogCassetteId: string | null;
  fogDocumentUri: string | null;
}

export interface LocalDevice {
  id: string;
  name: string;
  createdAtUtc: string;
  lastSeenAtUtc: string;
  expiresAtUtc: string;
  revokedAtUtc: string | null;
  current: boolean;
}

export interface LocalSession {
  authenticated: boolean;
  registrationEnabled: boolean;
  antiforgeryToken: string;
  user: LocalUser | null;
  devices: LocalDevice[];
}

export interface LocalAuthenticatedResponse {
  userId: string;
  login: string;
  displayName: string;
  roles: string[];
  fogCassetteId: string | null;
  fogDocumentUri: string | null;
  deviceId: string;
  expiresAtUtc: string;
}

export interface LocalLoginRequest {
  login: string;
  password: string;
  deviceName?: string;
}

export interface LocalRegisterRequest extends LocalLoginRequest {
  displayName?: string;
}
