import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { ProjectOverview } from "../api/models";
import { hasCassetteRight } from "../app/projectAccess";
import {
  useDocumentAsset,
  type DocumentPreviewPolicy
} from "../app/useDocumentAsset";
import { DocumentReplaceControl } from "./DocumentReplaceControl";

interface DocumentCardProps {
  uri: string;
  token: string;
  project: ProjectOverview | null;
  previewPolicy?: DocumentPreviewPolicy;
  imageDocument?: boolean;
  allowReplace?: boolean;
}

export function DocumentCard({
  uri,
  token,
  project,
  previewPolicy = "smallest",
  imageDocument = false,
  allowReplace = false
}: DocumentCardProps) {
  const asset = useDocumentAsset(uri, token, previewPolicy);
  const sourceCassetteId = asset.location?.cassetteId ?? null;
  const canReplace = allowReplace && sourceCassetteId !== null && hasCassetteRight(
    project,
    sourceCassetteId,
    "replaceDocuments"
  );

  async function openFile(): Promise<void> {
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
        {asset.loading && (
          <span className="muted">
            {imageDocument ? "Загрузка изображения…" : "Загрузка превью…"}
          </span>
        )}
        {asset.error && <span className="notice error">{asset.error}</span>}
        {!asset.loading && !asset.error && !asset.objectUrl && (
          <span className="file-placeholder">
            {imageDocument ? "Изображение недоступно" : "Превью недоступно"}
          </span>
        )}
        {asset.objectUrl && asset.contentType.startsWith("image/") && (
          <img
            src={asset.objectUrl}
            alt={imageDocument ? "Изображение" : "Предварительный просмотр документа"}
          />
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
        <strong>{imageDocument ? "Изображение" : "Документ"}</strong>
        {allowReplace && (
          <>
            <span className="muted mono">{uri}</span>
            {asset.location?.cassetteName && (
              <span className="muted">Кассета: {asset.location.cassetteName}</span>
            )}
          </>
        )}
        {asset.location?.originalAvailable && (
          <button className="button primary" type="button" onClick={openFile}>
            {imageDocument ? "Открыть изображение" : "Открыть файл"}
          </button>
        )}
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
