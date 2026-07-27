import type {
  ProjectCassetteOverview,
  ProjectOverview
} from "../api/models";

export function defaultWriteCassette(
  project: ProjectOverview | null
): ProjectCassetteOverview | null {
  if (project === null || project.defaultWriteCassetteId === null) {
    return null;
  }
  return project.cassettes.find(
    cassette => cassette.id === project.defaultWriteCassetteId
  ) ?? null;
}

export function cassettesWithRight(
  project: ProjectOverview | null,
  right: string
): ProjectCassetteOverview[] {
  return cassettesWithRights(project, [right]);
}

export function cassettesWithRights(
  project: ProjectOverview | null,
  rights: string[]
): ProjectCassetteOverview[] {
  return project?.cassettes.filter(cassette =>
    cassette.allowWrite && rights.every(right => cassette.rights.includes(right))
  ) ?? [];
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
