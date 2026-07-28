import { useMemo, useState } from "react";
import type {
  ProjectCassetteOverview,
  ResourcePortrait
} from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { relationRolesForType } from "../app/ontologyRelations";
import { relationDraft } from "../app/relationDraftFactory";
import { useOntologySchema } from "../app/useOntologySchema";
import { ResourceEditorHost } from "./ResourceEditorHost";
import { ResourceEditorLoadState } from "./ResourceEditorLoadState";

interface ComplexRelationCreatePaneProps {
  portrait: ResourcePortrait;
  cassettes: ProjectCassetteOverview[];
  cassetteId: string;
  token: string;
  onCancel: () => void;
  onSaved: (result: ResourceWriteResponse) => void;
}

export function ComplexRelationCreatePane(props: ComplexRelationCreatePaneProps) {
  const schemaState = useOntologySchema(props.token, true);
  const [selectedKey, setSelectedKey] = useState("");
  const roles = useMemo(
    () => schemaState.schema === null || props.portrait.type === null
      ? []
      : relationRolesForType(schemaState.schema, props.portrait.type),
    [schemaState.schema, props.portrait.type]
  );
  const selected = roles.find(role => role.key === selectedKey) ?? null;
  const relation = useMemo(
    () => selected === null
      ? null
      : relationDraft(
          selected,
          props.portrait.resourceId,
          props.cassetteId),
    [selected, props.portrait.resourceId, props.cassetteId]
  );

  if (schemaState.schema === null) {
    return (
      <ResourceEditorLoadState
        loading={schemaState.loading}
        error={schemaState.error}
        onCancel={props.onCancel}
      />
    );
  }

  if (selected === null || relation === null) {
    return (
      <section className="resource-editor complex-relation-select">
        <header className="resource-editor-title">
          <div>
            <span className="eyebrow">Связи</span>
            <h1>Добавить сложную связь</h1>
          </div>
          <button className="button subtle" type="button" onClick={props.onCancel}>
            Отмена
          </button>
        </header>
        <p className="muted">
          Выберите роль текущей сущности. Нужная служебная запись будет создана автоматически.
        </p>
        {roles.length === 0 ? (
          <div className="empty-state">
            <strong>Подходящих типов связей в онтологии нет</strong>
          </div>
        ) : (
          <label>
            <span>Тип связи и роль текущей сущности</span>
            <select value={selectedKey} onChange={event => setSelectedKey(event.target.value)}>
              <option value="">Выберите связь</option>
              {roles.map(role => (
                <option key={role.key} value={role.key}>{role.label}</option>
              ))}
            </select>
          </label>
        )}
      </section>
    );
  }

  return (
    <ResourceEditorHost
      mode="create"
      initialDraft={relation.draft}
      schema={schemaState.schema}
      cassettes={props.cassettes}
      token={props.token}
      title={`Новая связь: ${selected.relationType.label}`}
      typeLabel="Тип связи"
      lockType
      protectedRowIds={[relation.anchorRowId]}
      onCancel={props.onCancel}
      onSaved={props.onSaved}
    />
  );
}
