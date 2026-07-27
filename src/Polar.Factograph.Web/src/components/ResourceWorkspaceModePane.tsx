import type { ProjectCassetteOverview } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import type { ResourceDraft } from "../app/resourceDraftModels";
import { DocumentIntakePane } from "./DocumentIntakePane";
import { ResourceEditorPane } from "./ResourceEditorPane";

export type ResourceWorkspaceMode = "create" | "edit" | "document";

interface ResourceWorkspaceModePaneProps {
  mode: ResourceWorkspaceMode;
  initialDraft: ResourceDraft;
  writableCassettes: ProjectCassetteOverview[];
  documentCassettes: ProjectCassetteOverview[];
  token: string;
  onCancel: () => void;
  onSaved: (result: ResourceWriteResponse) => void;
}

export function ResourceWorkspaceModePane(props: ResourceWorkspaceModePaneProps) {
  if (props.mode === "document") {
    return (
      <DocumentIntakePane
        cassettes={props.documentCassettes}
        token={props.token}
        onCancel={props.onCancel}
        onSaved={props.onSaved}
      />
    );
  }

  return (
    <ResourceEditorPane
      mode={props.mode}
      initialDraft={props.initialDraft}
      cassettes={props.writableCassettes}
      token={props.token}
      onCancel={props.onCancel}
      onSaved={props.onSaved}
    />
  );
}
