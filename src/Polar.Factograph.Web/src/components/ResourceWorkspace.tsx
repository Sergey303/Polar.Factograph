import { useMemo, useState } from "react";
import type { ProjectOverview, ResourcePortrait } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { cassettesWithRight } from "../app/projectAccess";
import {
  emptyResourceDraft,
  resourceDraftFromPortrait
} from "../app/resourceDraftFactory";
import { preferredResourceCassette } from "../app/resourceEditorCassette";
import { ResourceEditorPane } from "./ResourceEditorPane";
import { ResourcePortraitView } from "./ResourcePortraitView";
import { ResourceWorkspaceActions } from "./ResourceWorkspaceActions";

type EditorMode = "create" | "edit" | null;

interface ResourceWorkspaceProps {
  project: ProjectOverview | null;
  portrait: ResourcePortrait | null;
  loading: boolean;
  error: string | null;
  token: string;
  onSelect: (resourceId: string) => void;
  onReload: () => void;
}

export function ResourceWorkspace(props: ResourceWorkspaceProps) {
  const [mode, setMode] = useState<EditorMode>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const writable = useMemo(
    () => cassettesWithRight(props.project, "writeMetadata"),
    [props.project]
  );
  const cassetteId = preferredResourceCassette(
    props.project,
    mode === "edit" ? props.portrait : null,
    writable
  );
  const initialDraft = useMemo(() =>
    mode === "edit" && props.portrait !== null
      ? resourceDraftFromPortrait(props.portrait, cassetteId)
      : emptyResourceDraft(cassetteId),
  [mode, props.portrait, cassetteId]);

  function saved(result: ResourceWriteResponse): void {
    setNotice(result.indexReady
      ? `Ревизия ${result.resourceId} сохранена.`
      : "Ревизия сохранена, но индекс требует восстановления.");
    setMode(null);
    const sameResource = props.portrait?.resourceId === result.resourceId;
    props.onSelect(result.resourceId);
    if (sameResource) props.onReload();
  }

  if (mode !== null) {
    return (
      <ResourceEditorPane
        mode={mode}
        initialDraft={initialDraft}
        cassettes={writable}
        token={props.token}
        onCancel={() => setMode(null)}
        onSaved={saved}
      />
    );
  }

  return (
    <>
      <ResourceWorkspaceActions
        canCreate={writable.length > 0}
        canEdit={writable.length > 0 &&
          props.portrait !== null && props.portrait.type !== null}
        notice={notice}
        onCreate={() => { setNotice(null); setMode("create"); }}
        onEdit={() => { setNotice(null); setMode("edit"); }}
      />
      <ResourcePortraitView
        portrait={props.portrait}
        loading={props.loading}
        error={props.error}
        token={props.token}
        project={props.project}
        onSelect={props.onSelect}
      />
    </>
  );
}