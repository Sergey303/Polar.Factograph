import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { ProjectOverview } from "../api/models";
import { hasCassetteRight } from "../app/projectAccess";
import { useDocumentAsset } from "../app/useDocumentAsset";
import { DocumentReplaceControl } from "./DocumentReplaceControl";

interface DocumentCardProps {
  uri: string;
  token: string;
  project: ProjectOverview | null;
}

export function DocumentCard({ uri, token, project }: DocumentCardProps) {
  const asset = useDocumentAsset(uri, token);
  const canReplace = asset.location !== null && hasCassetteRight(
    project,
    asset.location.cassetteId,
    "replaceDocuments"
  );

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
        {asset.location && <span className="muted">Кассета: {asset.location.cassetteName}</span>}
        <button className="button primary" onClick={openOriginal} disabled={!asset.location?.originalAvailable}>
          Открыть оригинал
        </button>
        <DocumentReplaceControl
          uri={uri}
          token={token}
          enabled={canReplace}
          onReplaced={asset.reload}
        />
      </div>
    </article>
  );
}
