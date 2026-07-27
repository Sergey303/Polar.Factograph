import { useState } from "react";
import type { DocumentWriteResponse } from "../api/documentWriteModels";
import type { OntologyWriteSchema } from "../api/ontologyModels";
import type { ProjectCassetteOverview } from "../api/models";
import {
  documentClasses,
  literalProperties,
  preferredUriProperty
} from "../app/documentIntakeDraft";
import { useDocumentUpload } from "../app/useDocumentUpload";

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
  const properties = literalProperties(selectedType);
  const uploader = useDocumentUpload(props.token, result =>
    props.onUploaded(result, typeId, uriPredicate));

  function changeType(nextTypeId: string): void {
    const nextType = classes.find(type => type.id === nextTypeId) ?? null;
    setTypeId(nextTypeId);
    setUriPredicate(preferredUriProperty(nextType)?.id ?? "");
  }

  return (
    <form
      className="document-upload-form"
      onSubmit={event => {
        event.preventDefault();
        if (typeId.length === 0 || uriPredicate.length === 0) return;
        void uploader.upload(file, cassetteId);
      }}
    >
      <header className="resource-editor-title">
        <div>
          <span className="eyebrow">Документы</span>
          <h1>Добавление документа</h1>
        </div>
        <button className="button subtle" type="button" onClick={props.onCancel}>Отмена</button>
      </header>

      <div className="notice">
        Сначала сохраняется оригинал. Затем мастер создаёт RDF-описание; при ошибке второй этап можно повторить без повторной загрузки файла.
      </div>

      <div className="document-upload-fields">
        <label>
          <span>Файл</span>
          <input type="file" onChange={event => setFile(event.target.files?.[0] ?? null)} />
        </label>
        <label>
          <span>Кассета файла и метаданных</span>
          <select value={cassetteId} onChange={event => setCassetteId(event.target.value)}>
            {props.cassettes.map(cassette => (
              <option key={cassette.id} value={cassette.id}>{cassette.name}</option>
            ))}
          </select>
        </label>
        <label>
          <span>Тип RDF-описания</span>
          <select value={typeId} onChange={event => changeType(event.target.value)}>
            <option value="">Выберите тип</option>
            {classes.map(type => <option key={type.id} value={type.id}>{type.label}</option>)}
          </select>
        </label>
        <label>
          <span>Свойство для iiss:// URI</span>
          <select value={uriPredicate} onChange={event => setUriPredicate(event.target.value)}>
            <option value="">Выберите свойство</option>
            {properties.map(property => (
              <option key={property.id} value={property.id}>{property.label}</option>
            ))}
          </select>
        </label>
      </div>

      {classes.length === 0 && <div className="notice error">В онтологии нет класса с литеральными свойствами.</div>}
      {uploader.error && <div className="notice error">{uploader.error}</div>}
      <footer className="resource-editor-actions">
        <button
          className="button primary"
          type="submit"
          disabled={uploader.busy || file === null || typeId.length === 0 || uriPredicate.length === 0}
        >
          {uploader.busy ? "Загрузка…" : "Загрузить оригинал"}
        </button>
      </footer>
    </form>
  );
}
