import { documentImageUrl } from "../api/factographApi";
import type { ProjectOverview, SemanticResourcePage } from "../api/models";
import {
  resourceDocumentUris,
  singleResourceDocumentUri
} from "../app/resourceDocuments";
import { CopyResourceLinkButton } from "./CopyResourceLinkButton";
import { DocumentSection } from "./DocumentSection";
import { LiteralFields } from "./LiteralFields";
import { ResourceDocumentMetadata } from "./ResourceDocumentMetadata";
import { SemanticResourceSections } from "./SemanticResourceSections";
import { TechnicalResourceDetails } from "./TechnicalResourceDetails";

interface ResourcePortraitViewProps {
  page: SemanticResourcePage | null;
  loading: boolean;
  error: string | null;
  token: string;
  project: ProjectOverview | null;
}

function titleOf(page: SemanticResourcePage): string {
  const named = page.portrait.literals.find(field =>
    /(^|[/#])(name|alias)$/i.test(field.predicate)
  );
  return named?.displayValue || page.portrait.resourceId;
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

export function ResourcePortraitView(props: ResourcePortraitViewProps) {
  if (props.loading) {
    return <div className="empty-state"><strong>Загрузка страницы…</strong></div>;
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
  const primaryDocument = singleResourceDocumentUri(portrait);
  const title = titleOf(page);
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
      <article className="portrait">
        <header className="portrait-header public-portrait-header">
          <div>
            <span className="eyebrow">{portrait.typeLabel ?? portrait.type ?? "Ресурс"}</span>
            <h1>{title}</h1>
          </div>
          <CopyResourceLinkButton resourceId={portrait.resourceId} />
        </header>

        {documents.length > 1 && (
          <div className="notice warning" role="status">
            У сущности указано несколько медиавложений. Они показаны ниже, но основное медиа не выбрано автоматически.
          </div>
        )}
        <DocumentSection
          uris={documents}
          token={props.token}
          project={props.project}
          title={null}
          previewPolicy="largest-preview"
        />
        <LiteralFields fields={portrait.literals} />
        <SemanticResourceSections page={page} />
        <TechnicalResourceDetails portrait={portrait} />
      </article>
    </>
  );
}
