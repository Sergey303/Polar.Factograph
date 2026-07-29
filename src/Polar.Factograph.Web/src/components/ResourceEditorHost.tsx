import { useMemo, useState } from "react";
import { createPortal } from "react-dom";
import type { OntologyWriteProperty } from "../api/ontologyModels";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { emptyResourceDraft } from "../app/resourceDraftFactory";
import { entityTypesMatchingRanges } from "../app/ontologyRelations";
import { ResourceEditor, type ResourceEditorProps } from "./ResourceEditor";

const NamePredicate = "http://fogid.net/o/name";

interface PendingReference {
  property: OntologyWriteProperty;
  onCreated: (resourceId: string) => void;
  initialValue?: string;
}

export function ResourceEditorHost(props: ResourceEditorProps) {
  const [pending, setPending] = useState<PendingReference | null>(null);
  const cassetteId = props.initialDraft.cassetteId || props.cassettes[0]?.id || "";
  const allowedTargetTypes = pending === null
    ? []
    : entityTypesMatchingRanges(props.schema, pending.property.ranges).map(type => type.id);
  const targetDraft = useMemo(() => {
    const draft = emptyResourceDraft(cassetteId);
    return allowedTargetTypes.length === 1
      ? { ...draft, typeId: allowedTargetTypes[0] ?? "" }
      : draft;
  }, [allowedTargetTypes.join("\n"), cassetteId, pending?.property.id]);
  const initialLiteralValues = useMemo<Readonly<Record<string, string>> | undefined>(() => {
    const value = pending?.initialValue?.trim();
    return value ? { [NamePredicate]: value } : undefined;
  }, [pending?.initialValue]);

  function useReference(resourceId: string): void {
    pending?.onCreated(resourceId);
    setPending(null);
  }

  function targetSaved(result: ResourceWriteResponse): void {
    useReference(result.resourceId);
  }

  return (
    <>
      <ResourceEditor
        {...props}
        onCreateReference={(property, onCreated, initialValue) =>
          setPending({ property, onCreated, initialValue })}
      />
      {pending !== null && createPortal(
        <div className="admin-overlay" role="presentation">
          <section
            className="reference-create-dialog"
            role="dialog"
            aria-modal="true"
            aria-label={`Создание сущности для свойства ${pending.property.label}`}
          >
            <ResourceEditor
              mode="create"
              initialDraft={targetDraft}
              schema={props.schema}
              cassettes={props.cassettes}
              token={props.token}
              title={`Новая сущность: ${pending.property.label}`}
              allowedTypeIds={allowedTargetTypes}
              initialLiteralValues={initialLiteralValues}
              onCancel={() => setPending(null)}
              onSaved={targetSaved}
              onUseExisting={useReference}
            />
          </section>
        </div>,
        document.body
      )}
    </>
  );
}
