import type { ApiError } from "./models";

let antiforgeryToken = "";

export class ApiRequestError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string
  ) {
    super(message);
    this.name = "ApiRequestError";
  }
}

export function setAntiforgeryToken(value: string): void {
  antiforgeryToken = value;
}

function headers(
  extra?: HeadersInit,
  includeAntiforgery = false
): Headers {
  const result = new Headers(extra);
  result.set("Accept", "application/json");
  if (includeAntiforgery && antiforgeryToken.length > 0) {
    result.set("X-CSRF-TOKEN", antiforgeryToken);
  }
  return result;
}

async function throwResponseError(response: Response): Promise<never> {
  let error: ApiError | null = null;
  try {
    error = (await response.json()) as ApiError;
  } catch {
    // Some infrastructure failures return an empty or non-JSON body.
  }

  throw new ApiRequestError(
    response.status,
    error?.code ?? "request_failed",
    error?.message ?? `HTTP ${response.status}`
  );
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    return throwResponseError(response);
  }
  return (await response.json()) as T;
}

export async function requestJson<T>(
  path: string,
  _sessionKey: string,
  signal?: AbortSignal
): Promise<T> {
  return readJson<T>(await fetch(path, {
    credentials: "same-origin",
    headers: headers(),
    signal
  }));
}

export async function requestJsonBody<T>(
  path: string,
  method: "POST" | "PUT",
  body: unknown,
  _sessionKey: string,
  signal?: AbortSignal
): Promise<T> {
  const response = await fetch(path, {
    method,
    credentials: "same-origin",
    headers: headers({ "Content-Type": "application/json" }, true),
    body: JSON.stringify(body),
    signal
  });
  return readJson<T>(response);
}

export async function requestEmpty(
  path: string,
  method: "POST" | "PUT",
  _sessionKey: string,
  signal?: AbortSignal
): Promise<void> {
  const response = await fetch(path, {
    method,
    credentials: "same-origin",
    headers: headers(undefined, true),
    signal
  });
  if (!response.ok) {
    return throwResponseError(response);
  }
}

export async function requestBinaryBody<T>(
  path: string,
  method: "POST" | "PUT",
  body: Blob,
  _sessionKey: string,
  signal?: AbortSignal
): Promise<T> {
  const response = await fetch(path, {
    method,
    credentials: "same-origin",
    headers: headers(undefined, true),
    body,
    signal
  });
  return readJson<T>(response);
}

export async function requestBlob(
  path: string,
  _sessionKey: string,
  signal?: AbortSignal
): Promise<Blob> {
  const response = await fetch(path, {
    credentials: "same-origin",
    headers: headers({ Accept: "*/*" }),
    signal
  });
  if (!response.ok) {
    return throwResponseError(response);
  }
  return response.blob();
}
