let fallbackSequence = 0;

/**
 * Creates a unique client-side identifier for transient form rows.
 * crypto.randomUUID is unavailable in non-secure HTTP contexts in some browsers,
 * so ordinary deployments must not depend on it being present.
 */
export function createClientId(): string {
  const cryptoApi = typeof globalThis.crypto === "object"
    ? globalThis.crypto
    : null;

  if (cryptoApi !== null && typeof cryptoApi.randomUUID === "function") {
    return cryptoApi.randomUUID();
  }

  if (cryptoApi !== null && typeof cryptoApi.getRandomValues === "function") {
    const words = new Uint32Array(4);
    cryptoApi.getRandomValues(words);
    return `local-${Array.from(
      words,
      word => word.toString(16).padStart(8, "0")
    ).join("-")}`;
  }

  fallbackSequence += 1;
  return [
    "local",
    Date.now().toString(36),
    fallbackSequence.toString(36),
    Math.random().toString(36).slice(2)
  ].join("-");
}
