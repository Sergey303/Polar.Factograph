import type { OntologyWriteSchema } from "../api/ontologyModels";
import { findWriteProperty } from "../app/ontologySchemaLookup";
import type { ResourcePropertyDraft } from "../app/resourceDraftModels";
import { ResourcePropertyRow } from "./ResourcePropertyRow";

interface ResourcePropertyListProps {
  typeId: string;
  rows: ResourcePropertyDraft[];
  schema: OntologyWriteSchema | null;
  onChange: (rowId: string, changes: Partial<ResourcePropertyDraft>) => void;
  onRemove: (rowId: string) => void;
}

export function ResourcePropertyList(props: ResourcePropertyListProps) {
  if (props.rows.length === 0) {
    return <p className="muted">У ресурса пока нет значений свойств.</p>;
  }

  return (
    <ul className="resource-property-list">
      {props.rows.map(row => (
        <ResourcePropertyRow
          key={row.rowId}
          row={row}
          property={findWriteProperty(props.schema, props.typeId, row.predicate)}
          onChange={changes => props.onChange(row.rowId, changes)}
          onRemove={() => props.onRemove(row.rowId)}
        />
      ))}
    </ul>
  );
}