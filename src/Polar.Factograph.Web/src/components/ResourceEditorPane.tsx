import type { ProjectCassetteOverview } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import type { ResourceDraft } from "../app/resourceDraftModels";
import { useOntologySchema } from "../app/useOntologySchema";
import { ResourceEditorHost } from "./ResourceEditorHost";
import { ResourceEditorLoadState } from "./ResourceEditorLoadState";

interface ResourceEditorPaneProps {
  mode: "create" | "edit";
  initialDraft: ResourceDraft;
  cassettes: ProjectCassetteOverview[];
  token: string;
  onCancel: () => void;
  onSaved: (result: ResourceWriteResponse) => void;
}

export function ResourceEditorPane(props: ResourceEditorPaneProps) {
  const schema = useOntologySchema(props.token, true);
  if (schema.schema === null) {
    return (
      <ResourceEditorLoadState
        loading={schema.loading}
        error={schema.error}
        onCancel={props.onCancel}
      />
    );
  }

  return (
    <ResourceEditorHost
      mode={props.mode}
      initialDraft={props.initialDraft}
      schema={schema.schema}
      cassettes={props.cassettes}
      token={props.token}
      onCancel={props.onCancel}
      onSaved={props.onSaved}
    />
  );
}
