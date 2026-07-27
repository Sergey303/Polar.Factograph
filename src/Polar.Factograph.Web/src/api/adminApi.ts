import { requestJson, requestJsonBody } from "./http";
import type {
  FogMaterializationStatistics,
  PreviewSubsystemStatus,
  ProjectIndexRebuildResult,
  ProjectIndexRuntimeStatus
} from "./adminModels";

export const adminApi = {
  getIndexStatus(token: string, signal?: AbortSignal): Promise<ProjectIndexRuntimeStatus> {
    return requestJson<ProjectIndexRuntimeStatus>(
      "/api/admin/index/status",
      token,
      signal
    );
  },

  getPreviewStatus(token: string, signal?: AbortSignal): Promise<PreviewSubsystemStatus> {
    return requestJson<PreviewSubsystemStatus>(
      "/api/admin/previews/status",
      token,
      signal
    );
  },

  getMaterializationSummary(
    token: string,
    signal?: AbortSignal
  ): Promise<FogMaterializationStatistics> {
    return requestJson<FogMaterializationStatistics>(
      "/api/admin/project/materialization-summary",
      token,
      signal
    );
  },

  rebuildIndex(token: string): Promise<ProjectIndexRebuildResult> {
    return requestJsonBody<ProjectIndexRebuildResult>(
      "/api/admin/index/rebuild",
      "POST",
      {},
      token
    );
  }
};
