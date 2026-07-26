import type { ResourcePortrait } from "../api/models";

export function resourceDocumentUris(portrait: ResourcePortrait): string[] {
  const values = portrait.literals
    .map(field => field.value.trim())
    .filter(value => value.startsWith("iiss://"));

  return [...new Set(values)].sort((left, right) =>
    left.localeCompare(right, "ru")
  );
}
