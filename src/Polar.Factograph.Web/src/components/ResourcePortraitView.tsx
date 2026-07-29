import type { ProjectOverview, SemanticResourcePage } from "../api/models";
import { resourceDocumentUris } from "../app/resourceDocuments";
import { CopyResourceLinkButton } from "./CopyResourceLinkButton";
import { DocumentSection } from "./DocumentSection";
import { LiteralFields } from "./LiteralFields";
import { SemanticResourceSections } from "./SemanticResourceSections";

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

  return (
    <article className="portrait">
      <header className="portrait-header public-portrait-header">
        <div>
          <span className="eyebrow">{portrait.typeLabel ?? portrait.type ?? "Ресурс"}</span>
          <h1>{titleOf(page)}</h1>
        </div>
        <CopyResourceLinkButton resourceId={portrait.resourceId} />
      </header>

      <DocumentSection
        uris={documents}
        token={props.token}
        project={props.project}
        title={null}
        previewPolicy="largest-preview"
      />
      <LiteralFields fields={portrait.literals} />
      <SemanticResourceSections page={page} />
    </article>
  );
}
