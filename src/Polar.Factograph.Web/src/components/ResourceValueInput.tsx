import type {
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";
import { ResourceReferenceInput } from "./ResourceReferenceInput";

interface ResourceValueInputProps {
  property: OntologyWriteProperty | null;
  schema: OntologyWriteSchema | null;
  token: string;
  value: string;
  readOnly?: boolean;
  onChange: (value: string) => void;
  onCreateReference?: (
    property: OntologyWriteProperty,
    onCreated: (resourceId: string) => void
  ) => void;
}

export function ResourceValueInput({
  property,
  schema,
  token,
  value,
  readOnly = false,
  onChange,
  onCreateReference
}: ResourceValueInputProps) {
  if (property?.kind === "resource" && schema !== null) {
    return (
      <ResourceReferenceInput
        property={property}
        schema={schema}
        token={token}
        value={value}
        readOnly={readOnly}
        onChange={onChange}
        onCreateNew={onCreateReference}
      />
    );
  }

  if (property !== null && property.options.length > 0) {
    return (
      <select
        value={value}
        disabled={readOnly}
        onChange={event => onChange(event.target.value)}
      >
        <option value="">Выберите значение</option>
        {property.options.map(option => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
    );
  }

  return (
    <input
      value={value}
      readOnly={readOnly}
      onChange={event => onChange(event.target.value)}
      placeholder={property?.kind === "resource" ? "Идентификатор сущности" : "Значение"}
    />
  );
}
