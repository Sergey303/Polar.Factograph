import { documentImageUrl } from "../api/factographApi";
import type {
  PresentedLiteral,
  ProjectOverview,
  SemanticResourcePage
} from "../api/models";
import {
  resourceDocumentUris,
  singleResourceDocumentUri
} from "../app/resourceDocuments";
import { CopyResourceLinkButton } from "./CopyResourceLinkButton";
import { DocumentLiteralSummary } from "./DocumentLiteralSummary";
import { DocumentSection } from "./DocumentSection";
import { ResourceDocumentMetadata } from "./ResourceDocumentMetadata";
import { ResourceLiteralSummary } from "./ResourceLiteralSummary";
import { SemanticResourceSections } from "./SemanticResourceSections";

const photoDocumentType = "http://fogid.net/o/photo-doc";

interface ResourcePortraitViewProps {
  page: SemanticResourcePage | null;
  loading: boolean;
  error: string | null;
  token: string;
  project: ProjectOverview | null;
}

function isNamePredicate(predicate: string): boolean {
  return /(^|[/#])(name|alias)$/i.test(predicate);
}

function titleOf(page: SemanticResourcePage, documentBacked: boolean): string {
  if (documentBacked) {
    return page.portrait.typeLabel ?? page.portrait.type ?? "Документ";
  }

  const named = page.portrait.literals.find(field => isNamePredicate(field.predicate));
  return named?.displayValue || page.portrait.resourceId;
}

function publicFields(
  fields: PresentedLiteral[],
  documentBacked: boolean,
  title: string
): PresentedLiteral[] {
  const normalizedTitle = title.trim().toLocaleLowerCase("ru-RU");
  return fields.filter(field => {
    const value = field.displayValue.trim();
    if (value.length === 0 || field.value.trim().startsWith("iiss://")) return false;
    if (!isNamePredicate(field.predicate)) return true;
    if (documentBacked) return false;
    return value.toLocaleLowerCase("ru-RU") !== normalizedTitle;
  });
}

function descriptionOf(page: SemanticResourcePage): string {
  const descriptive = page.portrait.literals.find(field =>
    /(^|[/#])(description|comment)$/i.test(field.predicate) &&
    field.displayValue.trim().length > 0
  );
  const fallback = page.portrait.typeLabel ?? page.portrait.type ?? "Ресурс";
  const value = descriptive?.displayValue.trim() || fallback;
  return value.length <= 240 ? value : `${value.slice(0, 237).trimEnd()}…`;
}

function ResourceLoading() {
  return (
    <div
      className="resource-loading"
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <span className="resource-loading-spinner" aria-hidden="true" />
      <div className="resource-loading-copy">
        <strong>Загрузка страницы…</strong>
        <span>Собираем основные сведения ресурса.</span>
      </div>
      <div className="resource-loading-progress" aria-hidden="true"><span /></div>
    </div>
  );
}

export function ResourcePortraitView(props: ResourcePortraitViewProps) {
  if (props.loading) {
    return <ResourceLoading />;
  }
  if (props.error) {
    return <div className="notice error portrait-error">{props.error}</div>;
  }
  if (!props.page) {
    return (
      <div className="empty-state portrait-empty">
        <strong>Сущность не загружена</strong>
        <span>Проверьте адрес страницы или выберите сущность через поиск в верхнем меню.</span>
      </div>
    );
  }

  const page = props.page;
  const portrait = page.portrait;
  const documents = resourceDocumentUris(portrait);
  const documentBacked = documents.length > 0;
  const photoDocument = portrait.type === photoDocumentType && documents.length > 0;
  const primaryDocument = singleResourceDocumentUri(portrait);
  const title = titleOf(page, documentBacked);
  const fields = publicFields(portrait.literals, documentBacked, title);
  const siteName = props.project?.name ?? "Polar.Factograph";
  const metadataImageUrl = primaryDocument === null
    ? null
    : documentImageUrl(primaryDocument);

  return (
    <>
      <ResourceDocumentMetadata
        resourceId={portrait.resourceId}
        title={title}
        description={descriptionOf(page)}
        siteName={siteName}
        imageUrl={metadataImageUrl}
      />
      <article className={`portrait${photoDocument ? " photo-document-portrait" : ""}`}>
        <header className="portrait-header public-portrait-header">
          <div className="portrait-title-block">
            <h1>{title}</h1>
            {!photoDocument && <ResourceLiteralSummary fields={fields} />}
          </div>
          <CopyResourceLinkButton resourceId={portrait.resourceId} />
        </header>

        {documents.length > 1 && (
          <div className="notice warning" role="status">
            У сущности указано несколько медиавложений. Они показаны ниже, но основное медиа не выбрано автоматически.
          </div>
        )}

        {photoDocument ? (
          <div className="photo-document-layout">
            <div className="photo-document-main">
              <DocumentSection
                uris={documents}
                token={props.token}
                project={props.project}
                title={null}
                previewPolicy="largest-preview"
                imageDocument
              />
              <DocumentLiteralSummary fields={fields} />
            </div>
            <aside className="photo-document-relations" aria-label="Связи фотографии">
              <SemanticResourceSections page={page} token={props.token} textOnly />
            </aside>
          </div>
        ) : (
          <>
            <DocumentSection
              uris={documents}
              token={props.token}
              project={props.project}
              title={null}
              previewPolicy="largest-preview"
            />
            <SemanticResourceSections page={page} token={props.token} />
          </>
        )}
      </article>
    </>
  );
}
