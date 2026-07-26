import type { OntologyWriteProperty } from "../api/ontologyModels";

interface ResourceValueInputProps {
  property: OntologyWriteProperty | null;
  value: string;
  onChange: (value: string) => void;
}

export function ResourceValueInput({
  property,
  value,
  onChange
}: ResourceValueInputProps) {
  if (property !== null && property.options.length > 0) {
    return (
      <select value={value} onChange={event => onChange(event.target.value)}>
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
      onChange={event => onChange(event.target.value)}
      placeholder={property?.kind === "resource" ? "Идентификатор ресурса" : "Значение"}
    />
  );
}