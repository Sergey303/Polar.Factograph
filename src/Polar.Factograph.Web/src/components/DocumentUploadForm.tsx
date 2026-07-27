import { useState } from "react";
import type { DocumentWriteResponse } from "../api/documentWriteModels";
import type { OntologyWriteSchema } from "../api/ontologyModels";
import type { ProjectCassetteOverview } from "../api/models";
import {
  documentClasses,
  preferredUriProperty,
  uriProperties
} from "../app/documentIntakeDraft";
import { useDocumentUpload } from "../app/useDocumentUpload";
import { DocumentUploadFields } from "./DocumentUploadFields";

interface DocumentUploadFormProps {
  schema: OntologyWriteSchema;
  cassettes: ProjectCassetteOverview[];
  token: string;
  onCancel: () => void;
  onUploaded: (
    result: DocumentWriteResponse,
    typeId: string,
    uriPredicate: string
  ) => void;
}

export function DocumentUploadForm(props: DocumentUploadFormProps) {
  const classes = documentClasses(props.schema);
  const firstType = classes[0] ?? null;
  const [file, setFile] = useState<File | null>(null);
  const [cassetteId, setCassetteId] = useState(props.cassettes[0]?.id ?? "");
  const [typeId, setTypeId] = useState(firstType?.id ?? "");
  const [uriPredicate, setUriPredicate] = useState(
    preferredUriProperty(firstType)?.id ?? ""
  );
  const selectedType = classes.find(type => type.id === typeId) ?? null;
  const uploader = useDocumentUpload(props.token, result =>
    props.onUploaded(result, typeId, uriPredicate));

  function changeType(nextTypeId: string): void {
    const nextType = classes.find(type => type.id === nextTypeId) ?? null;
    setTypeId(nextTypeId);
    setUriPredicate(preferredUriProperty(nextType)?.id ?? "");
  }

  const ready = file !== null && cassetteId.length > 0 &&
    typeId.length > 0 && uriPredicate.length > 0;
  return (
    <form
      className="document-upload-form"
      onSubmit={event => {
        event.preventDefault();
        if (ready) void uploader.upload(file, cassetteId);
      }}
    >
      <header className="resource-editor-title">
        <div>
          <span className="eyebrow">Документы</span>
          <h1>Добавление документа</h1>
        </div>
        <button
          className="button subtle"
          type="button"
          disabled={uploader.busy}
          onClick={props.onCancel}
        >
          Отмена
        </button>
      </header>

      <div className="notice">
        Сначала сохраняется оригинал. Затем мастер создаёт RDF-описание; при ошибке второй этап можно повторить без повторной загрузки файла.
      </div>
      <DocumentUploadFields
        cassettes={props.cassettes}
        classes={classes}
        properties={uriProperties(selectedType)}
        cassetteId={cassetteId}
        typeId={typeId}
        uriPredicate={uriPredicate}
        disabled={uploader.busy}
        onFileChange={setFile}
        onCassetteChange={setCassetteId}
        onTypeChange={changeType}
        onUriPredicateChange={setUriPredicate}
      />

      {classes.length === 0 && (
        <div className="notice error">В онтологии нет класса со свободным URI-свойством.</div>
      )}
      {uploader.error && <div className="notice error">{uploader.error}</div>}
      <footer className="resource-editor-actions">
        <button className="button primary" type="submit" disabled={uploader.busy || !ready}>
          {uploader.busy ? "Загрузка…" : "Загрузить оригинал"}
        </button>
      </footer>
    </form>
  );
}
