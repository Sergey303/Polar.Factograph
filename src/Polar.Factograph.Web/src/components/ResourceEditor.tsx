import { useEffect, useMemo } from "react";
import type {
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";
import type { ProjectCassetteOverview } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { findWriteClass } from "../app/ontologySchemaLookup";
import { newPropertyDraft } from "../app/resourceDraftFactory";
import type { ResourceDraft } from "../app/resourceDraftModels";
import { usePotentialDuplicateCheck } from "../app/usePotentialDuplicateCheck";
import { useResourceDraft } from "../app/useResourceDraft";
import { useResourceWrite } from "../app/useResourceWrite";
import { PotentialDuplicateWarning } from "./PotentialDuplicateWarning";
import { ResourceEditorHeader } from "./ResourceEditorHeader";
import { ResourcePropertyAdd } from "./ResourcePropertyAdd";
import { ResourcePropertyList } from "./ResourcePropertyList";

export interface ResourceEditorProps {
  mode: "create" | "edit";
  initialDraft: ResourceDraft;
  schema: OntologyWriteSchema;
  cassettes: ProjectCassetteOverview[];
  token: string;
  title?: string;
  typeLabel?: string;
  allowedTypeIds?: string[];
  lockType?: boolean;
  lockCassette?: boolean;
  protectedRowIds?: string[];
  onCancel: () => void;
  onSaved: (result: ResourceWriteResponse) => void;
  onUseExisting?: (resourceId: string) => void;
  onCreateReference?: (
    property: OntologyWriteProperty,
    onCreated: (resourceId: string) => void
  ) => void;
}

export function ResourceEditor(props: ResourceEditorProps) {
  const initialDraft = useMemo(() => {
    if (props.mode !== "create" || props.initialDraft.typeId.length === 0) {
      return props.initialDraft;
    }

    const initialType = findWriteClass(props.schema, props.initialDraft.typeId);
    if (initialType === null) return props.initialDraft;
    const existingPredicates = new Set(
      props.initialDraft.properties.map(property => property.predicate)
    );
    const missingRequired = initialType.properties
      .filter(property => property.isEssential && !existingPredicates.has(property.id))
      .map(newPropertyDraft);
    return missingRequired.length === 0
      ? props.initialDraft
      : {
          ...props.initialDraft,
          properties: [...props.initialDraft.properties, ...missingRequired]
        };
  }, [props.initialDraft, props.mode, props.schema]);
  const editor = useResourceDraft(initialDraft);
  const writer = useResourceWrite(props.token, props.schema, props.onSaved);
  const duplicates = usePotentialDuplicateCheck(props.token, props.schema);
  const type = findWriteClass(props.schema, editor.draft.typeId);
  const duplicateFingerprint = useMemo(
    () => [
      editor.draft.typeId,
      ...editor.draft.properties.map(row =>
        [row.predicate, row.kind, row.value, row.language, row.dataType].join("\u001f"))
    ].join("\n"),
    [editor.draft.properties, editor.draft.typeId]
  );

  useEffect(() => {
    duplicates.reset();
  }, [duplicateFingerprint, duplicates.reset]);

  function changeType(typeId: string): void {
    const nextType = findWriteClass(props.schema, typeId);
    editor.setType(
      typeId,
      nextType?.properties.filter(property => property.isEssential) ?? []
    );
  }

  async function saveWithDuplicateCheck(): Promise<void> {
    if (props.mode === "create") {
      const clear = await duplicates.check(editor.draft);
      if (!clear) return;
    }
    await writer.save(editor.draft);
  }

  function saveWithoutDuplicateCheck(): void {
    duplicates.reset();
    void writer.save(editor.draft);
  }

  const busy = writer.busy || duplicates.checking;

  return (
    <form
      className="resource-editor"
      onSubmit={event => {
        event.preventDefault();
        void saveWithDuplicateCheck();
      }}
    >
      <header className="resource-editor-title">
        <div>
          <span className="eyebrow">Метаданные</span>
          <h1>{props.title ?? (props.mode === "create" ? "Новая сущность" : "Редактирование сущности")}</h1>
        </div>
        <button className="button subtle" type="button" onClick={props.onCancel}>
          Отмена
        </button>
      </header>

      <ResourceEditorHeader
        mode={props.mode}
        draft={editor.draft}
        classes={props.schema.classes}
        cassettes={props.cassettes}
        lockType={props.lockType}
        lockCassette={props.lockCassette}
        allowedTypeIds={props.allowedTypeIds}
        typeLabel={props.typeLabel}
        onTypeChange={changeType}
        onFieldChange={editor.setField}
      />

      <section className="resource-editor-properties">
        <div className="section-heading-row">
          <h3>Свойства</h3>
          <ResourcePropertyAdd type={type} onAdd={editor.addProperty} />
        </div>
        <ResourcePropertyList
          typeId={editor.draft.typeId}
          rows={editor.draft.properties}
          schema={props.schema}
          token={props.token}
          protectedRowIds={props.protectedRowIds}
          onChange={editor.updateProperty}
          onRemove={editor.removeProperty}
          onCreateReference={props.onCreateReference}
        />
      </section>

      <PotentialDuplicateWarning
        candidates={duplicates.candidates}
        error={duplicates.error}
        onUseExisting={props.onUseExisting}
        onContinue={saveWithoutDuplicateCheck}
      />
      {writer.error && <div className="notice error">{writer.error}</div>}
      <footer className="resource-editor-actions">
        <button className="button primary" type="submit" disabled={busy}>
          {duplicates.checking
            ? "Проверка совпадений…"
            : writer.busy
              ? "Сохранение…"
              : "Сохранить новую ревизию"}
        </button>
      </footer>
    </form>
  );
}
