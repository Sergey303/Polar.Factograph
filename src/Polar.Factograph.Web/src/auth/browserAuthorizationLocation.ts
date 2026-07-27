const callbackParameters = [
  "code",
  "state",
  "session_state",
  "iss",
  "error",
  "error_description"
];

export function normalizedIssuer(value: string): string {
  return value.replace(/\/+$/, "");
}

export function currentRedirectUri(): string {
  return `${window.location.origin}${window.location.pathname}`;
}

export function hasAuthorizationCallback(): boolean {
  const parameters = new URLSearchParams(window.location.search);
  return parameters.has("code") || parameters.has("error");
}

export function clearAuthorizationCallback(url: URL): void {
  for (const name of callbackParameters) url.searchParams.delete(name);
  window.history.replaceState({}, document.title, url.toString());
}
