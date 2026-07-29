import type {
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";
import type { ResourcePropertyDraft } from "../app/resourceDraftModels";
import { ResourceValueInput } from "./ResourceValueInput";

interface ResourcePropertyRowProps {
  row: ResourcePropertyDraft;
  property: OntologyWriteProperty | null;
  schema: OntologyWriteSchema | null;
  token: string;
  protectedValue?: boolean;
  onChange: (changes: Partial<ResourcePropertyDraft>) => void;
  onRemove: () => void;
  onCreateReference?: (
    property: OntologyWriteProperty,
    onCreated: (resourceId: string) => void,
    initialValue?: string
  ) => void;
}

export function ResourcePropertyRow({
  row,
  property,
  schema,
  token,
  protectedValue = false,
  onChange,
  onRemove,
  onCreateReference
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
          {(protectedValue || property?.isEssential) && (
            <span className="badge accent">обязательно</span>
          )}
          {property === null && <span className="badge warning">неизвестно схеме</span>}
        </div>
      </div>

      <ResourceValueInput
        property={property}
        schema={schema}
        token={token}
        value={row.value}
        readOnly={protectedValue}
        onChange={value => onChange({ value })}
        onCreateReference={onCreateReference}
      />
      {ranges.length > 0 && <span className="muted">Допустимые типы: {ranges}</span>}

      {row.kind === "literal" && !protectedValue && (
        <label className="resource-property-language">
          <span>Язык текста</span>
          <input
            value={row.language}
            onChange={event => onChange({ language: event.target.value })}
            placeholder="Например, ru"
          />
        </label>
      )}
      {row.dataType && (
        <span className="muted mono">Тип данных задаётся онтологией: {row.dataType}</span>
      )}

      {!protectedValue && !property?.isEssential && (
        <button className="button danger compact" type="button" onClick={onRemove}>
          Удалить значение
        </button>
      )}
    </li>
  );
}
