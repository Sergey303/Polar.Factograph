import { useEffect, useMemo, useState } from "react";
import type { OntologyWriteSchema } from "../api/ontologyModels";
import type {
  ProjectCassetteOverview,
  ResourcePortrait
} from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import {
  relationRolesForType,
  type OntologyRelationRole
} from "../app/ontologyRelations";
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

export function ComplexRelationCreatePane(props: ComplexRelationCreatePaneProps) {
  const schemaState = useOntologySchema(props.token, true);
  const [state, setState] = useState<RelationEditorState>({ mode: "list" });
  const [existing, setExisting] = useState<ExistingRelation[]>([]);
  const [loadingExisting, setLoadingExisting] = useState(true);
  const [existingError, setExistingError] = useState<string | null>(null);
  const roles = useMemo(
    () => schemaState.schema === null || props.portrait.type === null
      ? []
      : relationRolesForType(schemaState.schema, props.portrait.type),
    [schemaState.schema, props.portrait.type]
  );

  useEffect(() => {
    const schema = schemaState.schema;
    if (schema === null) return;

    const controller = new AbortController();
    const sourceIds = [...new Set(
      props.portrait.inverseLinks.map(link => link.sourceResourceId)
    )];
    setLoadingExisting(true);
    setExistingError(null);

    async function load(currentSchema: OntologyWriteSchema): Promise<void> {
      const loaded = await Promise.allSettled(
        sourceIds.map(id => factographApi.getPortrait(id, props.token, controller.signal))
      );
      if (controller.signal.aborted) return;

      const relations: ExistingRelation[] = [];
      for (const result of loaded) {
        if (result.status !== "fulfilled") continue;
        const relationPortrait = result.value;
        const relationType = currentSchema.classes.find(type =>
          type.id === relationPortrait.type && !type.isEntityType && !type.isAbstract
        );
        if (relationType === undefined) continue;

        const anchorLink = relationPortrait.directLinks.find(link =>
          link.targetResourceId === props.portrait.resourceId &&
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

      relations.sort((left, right) =>
        left.role.label.localeCompare(right.role.label, "ru") ||
        left.portrait.resourceId.localeCompare(right.portrait.resourceId, "ru")
      );
      setExisting(relations);
    }

    load(schema)
      .catch(reason => {
        if (!controller.signal.aborted) setExistingError(errorText(reason));
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoadingExisting(false);
      });

    return () => controller.abort();
  }, [schemaState.schema, props.portrait, props.token]);

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
                    props.portrait.resourceId,
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
        row.value === props.portrait.resourceId)
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
