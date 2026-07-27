import type { OntologyWriteProperty } from "../api/ontologyModels";

interface ResourceValueInputProps {
  property: OntologyWriteProperty | null;
  value: string;
  readOnly?: boolean;
  onChange: (value: string) => void;
}

export function ResourceValueInput({
  property,
  value,
  readOnly = false,
  onChange
}: ResourceValueInputProps) {
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
      placeholder={property?.kind === "resource" ? "Идентификатор ресурса" : "Значение"}
    />
  );
}
