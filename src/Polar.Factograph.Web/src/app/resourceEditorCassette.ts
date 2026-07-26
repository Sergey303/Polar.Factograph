import type {
  ProjectCassetteOverview,
  ProjectOverview,
  ResourcePortrait
} from "../api/models";

export function preferredResourceCassette(
  project: ProjectOverview | null,
  portrait: ResourcePortrait | null,
  writable: ProjectCassetteOverview[]
): string {
  const sourceId = portrait?.provenance.sourceCassetteId;
  if (sourceId !== undefined && writable.some(item => item.id === sourceId)) {
    return sourceId;
  }

  const defaultId = project?.defaultWriteCassetteId;
  if (defaultId !== null && defaultId !== undefined &&
      writable.some(item => item.id === defaultId)) {
    return defaultId;
  }
  return writable[0]?.id ?? "";
}