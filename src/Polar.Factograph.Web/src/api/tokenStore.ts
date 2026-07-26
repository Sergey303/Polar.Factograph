const key = "polar.factograph.access-token";

export function readAccessToken(): string {
  return sessionStorage.getItem(key) ?? "";
}

export function writeAccessToken(value: string): void {
  const normalized = value.trim();
  if (normalized.length === 0) {
    sessionStorage.removeItem(key);
    return;
  }

  sessionStorage.setItem(key, normalized);
}
