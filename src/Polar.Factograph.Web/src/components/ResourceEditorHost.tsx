import { useMemo, useState } from "react";
import { createPortal } from "react-dom";
import type { OntologyWriteProperty } from "../api/ontologyModels";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { emptyResourceDraft } from "../app/resourceDraftFactory";
import { entityTypesMatchingRanges } from "../app/ontologyRelations";
import { ResourceEditor, type ResourceEditorProps } from "./ResourceEditor";

interface PendingReference {
  property: OntologyWriteProperty;
  onCreated: (resourceId: string) => void;
}

export function ResourceEditorHost(props: ResourceEditorProps) {
  const [pending, setPending] = useState<PendingReference | null>(null);
  const cassetteId = props.initialDraft.cassetteId || props.cassettes[0]?.id || "";
  const targetDraft = useMemo(
    () => emptyResourceDraft(cassetteId),
    [cassetteId, pending?.property.id]
  );
  const allowedTargetTypes = pending === null
    ? []
    : entityTypesMatchingRanges(props.schema, pending.property.ranges).map(type => type.id);

  function targetSaved(result: ResourceWriteResponse): void {
    pending?.onCreated(result.resourceId);
    setPending(null);
  }

  return (
    <>
      <ResourceEditor
        {...props}
        onCreateReference={(property, onCreated) => setPending({ property, onCreated })}
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
              onCancel={() => setPending(null)}
              onSaved={targetSaved}
            />
          </section>
        </div>,
        document.body
      )}
    </>
  );
}
