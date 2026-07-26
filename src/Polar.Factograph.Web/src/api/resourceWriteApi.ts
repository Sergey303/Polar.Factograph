import { requestJsonBody } from "./http";
import type {
  ResourceWriteRequest,
  ResourceWriteResponse
} from "./resourceWriteModels";

export const resourceWriteApi = {
  write(
    request: ResourceWriteRequest,
    token: string,
    signal?: AbortSignal
  ): Promise<ResourceWriteResponse> {
    return requestJsonBody<ResourceWriteResponse>(
      "/api/resources",
      "POST",
      request,
      token,
      signal
    );
  }
};