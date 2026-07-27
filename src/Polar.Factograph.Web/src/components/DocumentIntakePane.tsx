import { useMemo, useState } from "react";
import type { DocumentWriteResponse } from "../api/documentWriteModels";
import type { ProjectCassetteOverview } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { documentResourceDraft } from "../app/documentIntakeDraft";
import { useOntologySchema } from "../app/useOntologySchema";
import { DocumentUploadForm } from "./DocumentUploadForm";
import { DocumentUploadSummary } from "./DocumentUploadSummary";
import { ResourceEditor } from "./ResourceEditor";
import { ResourceEditorLoadState } from "./ResourceEditorLoadState";

interface PreparedDocument {
  upload: DocumentWriteResponse;
  typeId: string;
  uriPredicate: string;
}

interface DocumentIntakePaneProps {
  cassettes: ProjectCassetteOverview[];
  token: string;
  onCancel: () => void;
  onSaved: (result: ResourceWriteResponse) => void;
}

export function DocumentIntakePane(props: DocumentIntakePaneProps) {
  const [prepared, setPrepared] = useState<PreparedDocument | null>(null);
  const schema = useOntologySchema(props.token, true);
  const draft = useMemo(() => prepared === null ? null : documentResourceDraft(
    prepared.upload,
    prepared.typeId,
    prepared.uriPredicate
  ), [prepared]);

  if (schema.loading || schema.error || schema.schema === null) {
    return <ResourceEditorLoadState loading={schema.loading} error={schema.error} onCancel={props.onCancel} />;
  }

  if (prepared === null || draft === null) {
    return (
      <DocumentUploadForm
        schema={schema.schema}
        cassettes={props.cassettes}
        token={props.token}
        onCancel={props.onCancel}
        onUploaded={(upload, typeId, uriPredicate) =>
          setPrepared({ upload, typeId, uriPredicate })}
      />
    );
  }

  const current = prepared;
  const selectedCassette = props.cassettes.filter(
    cassette => cassette.id === current.upload.cassetteId
  );
  const protectedRowIds = draft.properties[0] === undefined
    ? []
    : [draft.properties[0].rowId];
  function cancelMetadata(): void {
    const leave = window.confirm(
      `Оригинал уже сохранён как ${current.upload.documentUri}. Выйти без RDF-описания?`
    );
    if (leave) props.onCancel();
  }

  return (
    <div className="document-metadata-stage">
      <DocumentUploadSummary upload={current.upload} />
      <ResourceEditor
        mode="create"
        initialDraft={draft}
        schema={schema.schema}
        cassettes={selectedCassette}
        token={props.token}
        title="Описание загруженного документа"
        lockType
        lockCassette
        protectedRowIds={protectedRowIds}
        onCancel={cancelMetadata}
        onSaved={props.onSaved}
      />
    </div>
  );
}
