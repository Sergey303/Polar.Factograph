import type { OntologyWriteProperty } from "../api/ontologyModels";
import type { ResourcePropertyDraft } from "../app/resourceDraftModels";
import { ResourceValueInput } from "./ResourceValueInput";

interface ResourcePropertyRowProps {
  row: ResourcePropertyDraft;
  property: OntologyWriteProperty | null;
  protectedValue?: boolean;
  onChange: (changes: Partial<ResourcePropertyDraft>) => void;
  onRemove: () => void;
}

export function ResourcePropertyRow({
  row,
  property,
  protectedValue = false,
  onChange,
  onRemove
}: ResourcePropertyRowProps) {
  const label = property?.label ?? row.predicate;
  const ranges = property?.ranges.join(", ") ?? "";

  return (
    <li className="resource-property-row">
      <div className="resource-property-heading">
        <div>
          <strong>{label}</strong>
          <span className="muted mono">{row.predicate}</span>
        </div>
        <div className="badge-row">
          <span className="badge">{row.kind === "resource" ? "связь" : "значение"}</span>
          {protectedValue && <span className="badge accent">обязательно</span>}
          {property === null && <span className="badge warning">неизвестно схеме</span>}
        </div>
      </div>

      <ResourceValueInput
        property={property}
        value={row.value}
        readOnly={protectedValue}
        onChange={value => onChange({ value })}
      />
      {ranges.length > 0 && <span className="muted">Диапазон: {ranges}</span>}

      {row.kind === "literal" && !protectedValue && (
        <div className="resource-property-metadata">
          <input
            value={row.language}
            onChange={event => onChange({ language: event.target.value })}
            placeholder="Язык, например ru"
          />
          <input
            value={row.dataType}
            onChange={event => onChange({ dataType: event.target.value })}
            placeholder="Тип данных, необязательно"
          />
        </div>
      )}

      {!protectedValue && (
        <button className="button danger compact" type="button" onClick={onRemove}>
          Удалить значение
        </button>
      )}
    </li>
  );
}
