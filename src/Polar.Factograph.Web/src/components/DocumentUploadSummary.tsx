import type { DocumentWriteResponse } from "../api/documentWriteModels";

interface DocumentUploadSummaryProps {
  upload: DocumentWriteResponse;
}

export function DocumentUploadSummary({ upload }: DocumentUploadSummaryProps) {
  const size = new Intl.NumberFormat("ru-RU").format(upload.length);
  return (
    <section className="document-upload-summary">
      <div>
        <span className="eyebrow">Оригинал сохранён</span>
        <strong>{upload.fileName}</strong>
        <span className="muted mono">{upload.documentUri}</span>
      </div>
      <div className="badge-row">
        <span className="badge">{size} байт</span>
        <span className="badge">превью: {upload.previewState}</span>
      </div>
      <span className="muted mono">SHA-256: {upload.sha256}</span>
    </section>
  );
}
