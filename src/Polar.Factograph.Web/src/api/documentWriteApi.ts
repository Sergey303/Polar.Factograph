import { requestBinaryBody } from "./http";
import type { DocumentWriteResponse } from "./documentWriteModels";

function query(values: Record<string, string>): string {
  const result = new URLSearchParams();
  for (const [name, value] of Object.entries(values)) {
    result.set(name, value);
  }
  return result.toString();
}

export const documentWriteApi = {
  add(file: File, cassetteId: string | null, token: string): Promise<DocumentWriteResponse> {
    const values: Record<string, string> = { fileName: file.name };
    if (cassetteId !== null) {
      values.cassetteId = cassetteId;
    }
    return requestBinaryBody<DocumentWriteResponse>(
      `/api/documents/files?${query(values)}`,
      "POST",
      file,
      token
    );
  },

  replace(uri: string, file: File, token: string): Promise<DocumentWriteResponse> {
    return requestBinaryBody<DocumentWriteResponse>(
      `/api/documents/files?${query({ uri, fileName: file.name })}`,
      "PUT",
      file,
      token
    );
  }
};
