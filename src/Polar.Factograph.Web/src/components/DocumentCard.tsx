import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { ProjectOverview } from "../api/models";
import { hasCassetteRight } from "../app/projectAccess";
import {
  useDocumentAsset,
  type DocumentPreviewPolicy
} from "../app/useDocumentAsset";
import { DocumentReplaceControl } from "./DocumentReplaceControl";
import { UiIcon } from "./UiIcon";

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
  const isImage = imageDocument || asset.contentType.startsWith("image/");
  const showMetadata = allowReplace && asset.location !== null;
  const hasActions = asset.location?.originalAvailable === true || canReplace;

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
    <article className={`document-card${showMetadata ? " with-metadata" : ""}`}>
      <div className="document-card-content">
        <div className="document-preview">
          {asset.loading && <span className="muted">Загрузка просмотра…</span>}
          {asset.error && <span className="notice error">{asset.error}</span>}
          {!asset.loading && !asset.error && !asset.objectUrl && (
            <span className="file-placeholder">Просмотр недоступен</span>
          )}
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
        {showMetadata && (
          <aside className="document-info">
            <h3>О документе</h3>
            <dl className="document-metadata">
              {asset.location?.cassetteName && (
                <div><dt>Кассета</dt><dd>{asset.location.cassetteName}</dd></div>
              )}
              <div><dt>Адрес</dt><dd className="mono">{uri}</dd></div>
            </dl>
          </aside>
        )}
      </div>
      {hasActions && (
        <div className="document-card-actions">
          {asset.location?.originalAvailable && (
            <button className="button primary" type="button" onClick={openFile}>
              <UiIcon name="external-link" />
              <span>{isImage ? "Открыть изображение" : "Открыть файл"}</span>
            </button>
          )}
          <DocumentReplaceControl
            uri={uri}
            token={token}
            enabled={canReplace}
            onReplaced={asset.reload}
          />
        </div>
      )}
    </article>
  );
}
