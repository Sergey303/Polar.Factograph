import type { ApiError } from "./models";

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

function headers(token: string, extra?: HeadersInit): Headers {
  const result = new Headers(extra);
  result.set("Accept", "application/json");
  if (token.trim().length > 0) {
    result.set("Authorization", `Bearer ${token.trim()}`);
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
  token: string,
  signal?: AbortSignal
): Promise<T> {
  return readJson<T>(await fetch(path, { headers: headers(token), signal }));
}

export async function requestJsonBody<T>(
  path: string,
  method: "POST" | "PUT",
  body: unknown,
  token: string,
  signal?: AbortSignal
): Promise<T> {
  const response = await fetch(path, {
    method,
    headers: headers(token, { "Content-Type": "application/json" }),
    body: JSON.stringify(body),
    signal
  });
  return readJson<T>(response);
}

export async function requestBinaryBody<T>(
  path: string,
  method: "POST" | "PUT",
  body: Blob,
  token: string,
  signal?: AbortSignal
): Promise<T> {
  const response = await fetch(path, {
    method,
    headers: headers(token),
    body,
    signal
  });
  return readJson<T>(response);
}

export async function requestBlob(
  path: string,
  token: string,
  signal?: AbortSignal
): Promise<Blob> {
  const response = await fetch(path, {
    headers: headers(token, { Accept: "*/*" }),
    signal
  });
  if (!response.ok) {
    return throwResponseError(response);
  }
  return response.blob();
}
