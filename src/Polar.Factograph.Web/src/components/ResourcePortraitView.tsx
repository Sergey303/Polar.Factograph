import type { ProjectOverview, SemanticResourcePage } from "../api/models";
import { resourceDocumentUris } from "../app/resourceDocuments";
import { DocumentSection } from "./DocumentSection";
import { LiteralFields } from "./LiteralFields";
import { SemanticResourceSections } from "./SemanticResourceSections";

const photoDocumentType = "http://fogid.net/o/photo-doc";

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
        <strong>Ресурс не выбран</strong>
        <span>Вернитесь к поиску и откройте нужную сущность.</span>
      </div>
    );
  }

  const page = props.page;
  const portrait = page.portrait;
  const documents = resourceDocumentUris(portrait);
  const modified = new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(portrait.provenance.modifiedAt));

  return (
    <article className="portrait">
      <header className="portrait-header">
        <div>
          <span className="eyebrow">{portrait.typeLabel ?? portrait.type ?? "Ресурс"}</span>
          <h1>{titleOf(page)}</h1>
          <span className="muted mono">{portrait.resourceId}</span>
        </div>
        <div className="provenance">
          <span>{modified}</span>
        </div>
      </header>

      <LiteralFields fields={portrait.literals} />
      <DocumentSection
        uris={documents}
        token={props.token}
        project={props.project}
        previewOnly={portrait.type === photoDocumentType}
      />
      <SemanticResourceSections page={page} />
    </article>
  );
}
