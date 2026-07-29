import { useQueries, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import type {
  ProjectCassetteOverview,
  ResourcePortrait
} from "../api/models";
import type { OntologyWriteSchema } from "../api/ontologyModels";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { errorText } from "../api/errorText";
import {
  relationRolesForType,
  type OntologyRelationRole
} from "../app/ontologyRelations";
import { portraitQueryOptions } from "../app/queryOptions";
import {
  relationDraft,
  type RelationDraftResult
} from "../app/relationDraftFactory";
import { resourceDraftFromPortrait } from "../app/resourceDraftFactory";
import type { ResourceDraft } from "../app/resourceDraftModels";
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

interface ExistingRelation {
  portrait: ResourcePortrait;
  role: OntologyRelationRole;
}

type RelationEditorState =
  | { mode: "list" }
  | { mode: "choose" }
  | { mode: "create"; role: OntologyRelationRole; relation: RelationDraftResult }
  | {
      mode: "edit";
      relation: ExistingRelation;
      draft: ResourceDraft;
      protectedRowIds: string[];
    };

function existingRelations(
  schema: OntologyWriteSchema | null,
  portraits: readonly (ResourcePortrait | undefined)[],
  currentResourceId: string
): ExistingRelation[] {
  if (schema === null) return [];

  const relations: ExistingRelation[] = [];
  for (const relationPortrait of portraits) {
    if (relationPortrait === undefined) continue;
    const relationType = schema.classes.find(type =>
      type.id === relationPortrait.type && !type.isEntityType && !type.isAbstract
    );
    if (relationType === undefined) continue;

    const anchorLink = relationPortrait.directLinks.find(link =>
      link.targetResourceId === currentResourceId &&
      relationType.properties.some(property =>
        property.id === link.predicate && property.kind === "resource")
    );
    if (anchorLink === undefined) continue;

    const anchorProperty = relationType.properties.find(
      property => property.id === anchorLink.predicate
    );
    if (anchorProperty === undefined) continue;

    relations.push({
      portrait: relationPortrait,
      role: {
        key: `${relationType.id}\n${anchorProperty.id}`,
        relationType,
        anchorProperty,
        label: `${relationType.label}: ${anchorProperty.inverseLabel ?? anchorProperty.label}`
      }
    });
  }

  return relations.sort((left, right) =>
    left.role.label.localeCompare(right.role.label, "ru") ||
    left.portrait.resourceId.localeCompare(right.portrait.resourceId, "ru")
  );
}

export function ComplexRelationCreatePane(props: ComplexRelationCreatePaneProps) {
  const schemaState = useOntologySchema(props.token, true);
  const [state, setState] = useState<RelationEditorState>({ mode: "list" });
  const currentResourceId = props.portrait.resourceId;
  const currentPortraitQuery = useQuery(
    portraitQueryOptions(currentResourceId, props.token)
  );
  const currentPortrait = currentPortraitQuery.data ?? props.portrait;
  const sourceIds = [...new Set(
    currentPortrait.inverseLinks.map(link => link.sourceResourceId)
  )];
  const relationQueries = useQueries({
    queries: sourceIds.map(resourceId =>
      portraitQueryOptions(resourceId, props.token))
  });
  const roles = schemaState.schema === null || currentPortrait.type === null
    ? []
    : relationRolesForType(schemaState.schema, currentPortrait.type);
  const existing = existingRelations(
    schemaState.schema,
    relationQueries.map(query => query.data),
    currentResourceId
  );
  const loadingExisting = currentPortraitQuery.isFetching ||
    relationQueries.some(query => query.isFetching);
  const existingErrors = [
    currentPortraitQuery.error,
    ...relationQueries.map(query => query.error)
  ].filter(error => error !== null);
  const existingError = existingErrors.length === 0
    ? null
    : [...new Set(existingErrors.map(errorText))].join(" · ");

  if (schemaState.schema === null) {
    return (
      <ResourceEditorLoadState
        loading={schemaState.loading}
        error={schemaState.error}
        onCancel={props.onCancel}
      />
    );
  }

  const schema = schemaState.schema;

  if (state.mode === "create") {
    return (
      <ResourceEditorHost
        mode="create"
        initialDraft={state.relation.draft}
        schema={schema}
        cassettes={props.cassettes}
        token={props.token}
        title={`Новая связь: ${state.role.relationType.label}`}
        typeLabel="Тип связи"
        lockType
        protectedRowIds={[state.relation.anchorRowId]}
        onCancel={() => setState({ mode: "choose" })}
        onSaved={props.onSaved}
      />
    );
  }

  if (state.mode === "edit") {
    return (
      <ResourceEditorHost
        mode="edit"
        initialDraft={state.draft}
        schema={schema}
        cassettes={props.cassettes}
        token={props.token}
        title={`Редактирование связи: ${state.relation.role.relationType.label}`}
        typeLabel="Тип связи"
        lockType
        protectedRowIds={state.protectedRowIds}
        onCancel={() => setState({ mode: "list" })}
        onSaved={props.onSaved}
      />
    );
  }

  if (state.mode === "choose") {
    return (
      <section className="resource-editor complex-relation-select">
        <header className="resource-editor-title">
          <div>
            <span className="eyebrow">Связи</span>
            <h1>Добавить сложную связь</h1>
          </div>
          <button className="button subtle" type="button" onClick={() => setState({ mode: "list" })}>
            Назад
          </button>
        </header>
        <p className="muted">
          Выберите роль текущей сущности. Служебная запись связи будет создана автоматически.
        </p>
        {roles.length === 0 ? (
          <div className="empty-state">
            <strong>Подходящих типов связей в онтологии нет</strong>
          </div>
        ) : (
          <div className="relation-choice-list">
            {roles.map(role => (
              <button
                key={role.key}
                className="relation-row"
                type="button"
                onClick={() => setState({
                  mode: "create",
                  role,
                  relation: relationDraft(
                    role,
                    currentResourceId,
                    props.cassetteId)
                })}
              >
                <strong>{role.relationType.label}</strong>
                <span>{role.anchorProperty.inverseLabel ?? role.anchorProperty.label}</span>
              </button>
            ))}
          </div>
        )}
      </section>
    );
  }

  function editRelation(relation: ExistingRelation): void {
    const sourceCassette = relation.portrait.provenance.sourceCassetteId;
    const relationCassetteId = props.cassettes.some(item => item.id === sourceCassette)
      ? sourceCassette
      : props.cassetteId;
    const draft = resourceDraftFromPortrait(relation.portrait, relationCassetteId);
    const protectedRowIds = draft.properties
      .filter(row =>
        row.kind === "resource" &&
        row.predicate === relation.role.anchorProperty.id &&
        row.value === currentResourceId)
      .map(row => row.rowId);
    setState({ mode: "edit", relation, draft, protectedRowIds });
  }

  return (
    <section className="resource-editor complex-relation-select">
      <header className="resource-editor-title">
        <div>
          <span className="eyebrow">Связи</span>
          <h1>Сложные связи сущности</h1>
        </div>
        <button className="button subtle" type="button" onClick={props.onCancel}>
          Закрыть
        </button>
      </header>
      <div className="section-heading-row">
        <div>
          <h3>Существующие связи</h3>
          <p className="muted">Связи хранятся отдельными служебными сущностями, но здесь редактируются как обычные отношения.</p>
        </div>
        <button className="button primary" type="button" onClick={() => setState({ mode: "choose" })}>
          Добавить связь
        </button>
      </div>
      {loadingExisting && <span className="muted">Загрузка связей…</span>}
      {existingError && <div className="notice error">{existingError}</div>}
      {!loadingExisting && !existingError && existing.length === 0 && (
        <div className="empty-state">
          <strong>Сложных связей пока нет</strong>
          <span>Добавьте отражение на фотографии, участие, элемент коллекции или другую связь из онтологии.</span>
        </div>
      )}
      {existing.length > 0 && (
        <div className="relation-choice-list">
          {existing.map(relation => (
            <button
              key={relation.portrait.resourceId}
              className="relation-row"
              type="button"
              onClick={() => editRelation(relation)}
            >
              <strong>{relation.role.label}</strong>
              <span className="muted mono">{relation.portrait.resourceId}</span>
              <span>Редактировать</span>
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
