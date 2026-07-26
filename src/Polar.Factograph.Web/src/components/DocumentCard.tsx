import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import { useDocumentAsset } from "../app/useDocumentAsset";

interface DocumentCardProps {
  uri: string;
  token: string;
}

export function DocumentCard({ uri, token }: DocumentCardProps) {
  const asset = useDocumentAsset(uri, token);

  async function openOriginal(): Promise<void> {
    try {
      const blob = await factographApi.getDocumentBlob(uri, "original", token);
      const url = URL.createObjectURL(blob);
      window.open(url, "_blank", "noopener,noreferrer");
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (reason) {
      window.alert(errorText(reason));
    }
  }

  return (
    <article className="document-card">
      <div className="document-preview">
        {asset.loading && <span className="muted">Загрузка превью…</span>}
        {asset.error && <span className="notice error">{asset.error}</span>}
        {asset.objectUrl && asset.contentType.startsWith("image/") && (
          <img src={asset.objectUrl} alt="Предварительный просмотр документа" />
        )}
        {asset.objectUrl && asset.contentType === "application/pdf" && (
          <iframe src={asset.objectUrl} title="Предварительный просмотр PDF" />
        )}
        {asset.objectUrl &&
          !asset.contentType.startsWith("image/") &&
          asset.contentType !== "application/pdf" && (
            <span className="file-placeholder">Файл готов к открытию</span>
          )}
      </div>
      <div className="document-info">
        <strong>{asset.location?.documentNumber ?? "Документ"}</strong>
        <span className="muted mono">{uri}</span>
        {asset.location && (
          <span className="muted">Кассета: {asset.location.cassetteName}</span>
        )}
        <button
          className="button primary"
          onClick={openOriginal}
          disabled={!asset.location?.originalAvailable}
        >
          Открыть оригинал
        </button>
      </div>
    </article>
  );
}
