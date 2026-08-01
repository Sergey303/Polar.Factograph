import type { ResourcePortrait } from "../api/models";

export function isDocumentUri(value: string): boolean {
  try {
    const uri = new URL(value);
    if (uri.protocol.toLocaleLowerCase("en-US") !== "iiss:" || uri.username.length === 0) {
      return false;
    }

    const segments = uri.pathname
      .split("/")
      .map(segment => segment.trim())
      .filter(segment => segment.length > 0);
    if (segments.length < 2) return false;

    const folder = segments.at(-2) ?? "";
    const document = segments.at(-1) ?? "";
    return folder.length === 4 && document.length === 4;
  } catch {
    return false;
  }
}

export function resourceDocumentUris(portrait: ResourcePortrait): string[] {
  const values = portrait.literals
    .map(field => field.value.trim())
    .filter(isDocumentUri);

  return [...new Set(values)].sort((left, right) =>
    left.localeCompare(right, "ru")
  );
}

export function singleResourceDocumentUri(
  portrait: ResourcePortrait
): string | null {
  const values = resourceDocumentUris(portrait);
  return values.length === 1 ? values[0] ?? null : null;
}
