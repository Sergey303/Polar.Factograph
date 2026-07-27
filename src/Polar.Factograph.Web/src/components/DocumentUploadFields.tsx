import type { OntologyWriteClass, OntologyWriteProperty } from "../api/ontologyModels";
import type { ProjectCassetteOverview } from "../api/models";

interface DocumentUploadFieldsProps {
  cassettes: ProjectCassetteOverview[];
  classes: OntologyWriteClass[];
  properties: OntologyWriteProperty[];
  cassetteId: string;
  typeId: string;
  uriPredicate: string;
  disabled?: boolean;
  onFileChange: (file: File | null) => void;
  onCassetteChange: (value: string) => void;
  onTypeChange: (value: string) => void;
  onUriPredicateChange: (value: string) => void;
}

export function DocumentUploadFields(props: DocumentUploadFieldsProps) {
  return (
    <div className="document-upload-fields">
      <label>
        <span>Файл</span>
        <input
          type="file"
          disabled={props.disabled}
          onChange={event => props.onFileChange(event.target.files?.[0] ?? null)}
        />
      </label>
      <label>
        <span>Кассета файла и метаданных</span>
        <select
          value={props.cassetteId}
          disabled={props.disabled}
          onChange={event => props.onCassetteChange(event.target.value)}
        >
          {props.cassettes.map(cassette => (
            <option key={cassette.id} value={cassette.id}>{cassette.name}</option>
          ))}
        </select>
      </label>
      <label>
        <span>Тип RDF-описания</span>
        <select
          value={props.typeId}
          disabled={props.disabled}
          onChange={event => props.onTypeChange(event.target.value)}
        >
          <option value="">Выберите тип</option>
          {props.classes.map(type => (
            <option key={type.id} value={type.id}>{type.label}</option>
          ))}
        </select>
      </label>
      <label>
        <span>Свойство для iiss:// URI</span>
        <select
          value={props.uriPredicate}
          disabled={props.disabled}
          onChange={event => props.onUriPredicateChange(event.target.value)}
        >
          <option value="">Выберите свойство</option>
          {props.properties.map(property => (
            <option key={property.id} value={property.id}>{property.label}</option>
          ))}
        </select>
      </label>
    </div>
  );
}
