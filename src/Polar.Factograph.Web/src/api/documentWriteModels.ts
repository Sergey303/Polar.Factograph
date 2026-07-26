export interface DocumentWriteResponse {
  cassetteId: string;
  cassetteName: string;
  documentUri: string;
  folderName: string;
  documentNumber: string;
  fileName: string;
  length: number;
  sha256: string;
  replaced: boolean;
  previewState: string;
  previewRequestId: string | null;
  previewQueuedAtUtc: string | null;
}
