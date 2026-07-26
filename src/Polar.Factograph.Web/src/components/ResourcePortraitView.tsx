import type { ResourcePortrait } from "../api/models";
import { resourceDocumentUris } from "../app/resourceDocuments";
import { DocumentSection } from "./DocumentSection";
import { LiteralFields } from "./LiteralFields";
import { RelationSection } from "./RelationSection";

interface ResourcePortraitViewProps {
  portrait: ResourcePortrait | null;
  loading: boolean;
  error: string | null;
  token: string;
  onSelect: (resourceId: string) => void;
}

function titleOf(portrait: ResourcePortrait): string {
  const named = portrait.literals.find(field =>
    /(^|[/#])(name|alias)$/i.test(field.predicate)
  );
  return named?.displayValue || portrait.resourceId;
}

export function ResourcePortraitView(props: ResourcePortraitViewProps) {
  if (props.loading) {
    return <div className="empty-state"><strong>Загрузка карточки…</strong></div>;
  }
  if (props.error) {
    return <div className="notice error portrait-error">{props.error}</div>;
  }
  if (!props.portrait) {
    return (
      <div className="empty-state portrait-empty">
        <strong>Выберите ресурс</strong>
        <span>Здесь появятся поля, связи и документы.</span>
      </div>
    );
  }

  const portrait = props.portrait;
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
          <h1>{titleOf(portrait)}</h1>
          <span className="muted mono">{portrait.resourceId}</span>
        </div>
        <div className="provenance">
          <span>{portrait.provenance.sourceCassetteId}</span>
          <span>{modified}</span>
        </div>
      </header>

      <LiteralFields fields={portrait.literals} />
      <DocumentSection uris={documents} token={props.token} />
      <RelationSection
        direct={portrait.directLinks}
        inverse={portrait.inverseLinks}
        onSelect={props.onSelect}
      />
    </article>
  );
}
