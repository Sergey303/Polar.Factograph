import { useEffect, useState } from "react";
import type {
  OntologyWriteClass,
  OntologyWriteProperty
} from "../api/ontologyModels";

interface ResourcePropertyAddProps {
  type: OntologyWriteClass | null;
  onAdd: (property: OntologyWriteProperty) => void;
}

export function ResourcePropertyAdd({ type, onAdd }: ResourcePropertyAddProps) {
  const [propertyId, setPropertyId] = useState("");

  useEffect(() => {
    setPropertyId(type?.properties[0]?.id ?? "");
  }, [type]);

  if (type === null || type.properties.length === 0) return null;

  function add(): void {
    const property = type?.properties.find(item => item.id === propertyId);
    if (property !== undefined) onAdd(property);
  }

  return (
    <div className="resource-property-add">
      <select value={propertyId} onChange={event => setPropertyId(event.target.value)}>
        {type.properties.map(property => (
          <option key={property.id} value={property.id}>
            {property.label} · {property.kind === "resource" ? "связь" : "значение"}
          </option>
        ))}
      </select>
      <button className="button subtle" type="button" onClick={add}>
        Добавить свойство
      </button>
    </div>
  );
}