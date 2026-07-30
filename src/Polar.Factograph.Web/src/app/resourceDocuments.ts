import type { ResourcePortrait } from "../api/models";

export function resourceDocumentUris(portrait: ResourcePortrait): string[] {
  const values = portrait.literals
    .map(field => field.value.trim())
    .filter(value => value.startsWith("iiss://"));

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
