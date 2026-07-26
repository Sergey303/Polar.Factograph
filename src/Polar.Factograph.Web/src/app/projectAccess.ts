import type {
  ProjectCassetteOverview,
  ProjectOverview
} from "../api/models";

export function defaultWriteCassette(
  project: ProjectOverview | null
): ProjectCassetteOverview | null {
  if (project?.defaultWriteCassetteId === null || project === null) {
    return null;
  }
  return project.cassettes.find(
    cassette => cassette.id === project.defaultWriteCassetteId
  ) ?? null;
}

export function hasDefaultCassetteRight(
  project: ProjectOverview | null,
  right: string
): boolean {
  return defaultWriteCassette(project)?.rights.includes(right) ?? false;
}

export function hasCassetteRight(
  project: ProjectOverview | null,
  cassetteId: string,
  right: string
): boolean {
  return project?.cassettes.find(cassette => cassette.id === cassetteId)
    ?.rights.includes(right) ?? false;
}
