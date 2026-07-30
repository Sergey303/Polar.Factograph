export interface ProjectIndexRuntimeStatus {
  state: string;
  dirty: boolean;
  dirtySinceUtc: string | null;
  currentPointerState: string;
  currentGenerationId: string | null;
  currentGenerationAvailable: boolean;
  completedGenerationCount: number;
  buildingGenerationCount: number;
}

export interface ProjectIndexBuildStatistics {
  resources: number;
  triples: number;
  nameSearchRows: number;
  wordSearchRows: number;
}

export interface ProjectIndexRebuildResult {
  generationId: string;
  sourceFiles: number;
  statistics: ProjectIndexBuildStatistics;
}

export interface CassettePreviewQueueStatus {
  cassetteId: string;
  cassetteName: string;
  queued: number;
  processing: number;
  failed: number;
  oldestQueuedAtUtc: string | null;
}

export interface ProjectPreviewQueueStatus {
  queued: number;
  processing: number;
  failed: number;
  cassettes: CassettePreviewQueueStatus[];
}

export interface PreviewWorkerRuntimeSnapshot {
  state: string;
  enabled: boolean;
  startedAtUtc: string | null;
  stoppedAtUtc: string | null;
  lastCycleStartedAtUtc: string | null;
  lastCycleCompletedAtUtc: string | null;
  lastSuccessAtUtc: string | null;
  lastFailureAtUtc: string | null;
  lastHandled: number;
  totalHandled: number;
  consecutiveFailures: number;
  lastFailureCode: string | null;
}

export interface PreviewWorkerHealth {
  state: string;
  enabled: boolean;
  degraded: boolean;
}

export interface PreviewSubsystemStatus {
  queue: ProjectPreviewQueueStatus;
  worker: PreviewWorkerRuntimeSnapshot;
  health: PreviewWorkerHealth;
}

export interface FogMaterializationStatistics {
  sourceFiles: number;
  sourceRecords: number;
  resourceDefinitions: number;
  deleteOperations: number;
  substituteOperations: number;
  duplicateResourceIds: number;
  redirectedIds: number;
  deletedIds: number;
  currentSourceResources: number;
  syntheticResources: number;
  currentProperties: number;
}

export type OntologyValidationSeverity = "error" | "warning";

export interface OntologyValidationIssue {
  severity: OntologyValidationSeverity;
  code: string;
  termId: string;
  message: string;
}

export interface OntologyValidationReport {
  termCount: number;
  errorCount: number;
  warningCount: number;
  issues: OntologyValidationIssue[];
  isValid: boolean;
}
