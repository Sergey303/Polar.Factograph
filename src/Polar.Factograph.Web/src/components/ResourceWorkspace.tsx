import { useMemo, useState } from "react";
import type { ProjectOverview, SemanticResourcePage } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { cassettesWithRight, cassettesWithRights } from "../app/projectAccess";
import {
  emptyResourceDraft,
  resourceDraftFromPortrait
} from "../app/resourceDraftFactory";
import { preferredResourceCassette } from "../app/resourceEditorCassette";
import { ResourcePortraitView } from "./ResourcePortraitView";
import { ResourceWorkspaceActions } from "./ResourceWorkspaceActions";
import {
  ResourceWorkspaceModePane,
  type ResourceWorkspaceMode
} from "./ResourceWorkspaceModePane";

interface ResourceWorkspaceProps {
  project: ProjectOverview | null;
  page: SemanticResourcePage | null;
  loading: boolean;
  error: string | null;
  token: string;
  onSelect: (resourceId: string) => void;
  onReload: () => void;
}

export function ResourceWorkspace(props: ResourceWorkspaceProps) {
  const [mode, setMode] = useState<ResourceWorkspaceMode | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const portrait = props.page?.portrait ?? null;
  const writable = useMemo(
    () => cassettesWithRight(props.project, "writeMetadata"),
    [props.project]
  );
  const documentCassettes = useMemo(
    () => cassettesWithRights(props.project, ["addDocuments", "writeMetadata"]),
    [props.project]
  );
  const cassetteId = preferredResourceCassette(
    props.project,
    mode === "edit" ? portrait : null,
    writable
  );
  const initialDraft = useMemo(() =>
    mode === "edit" && portrait !== null
      ? resourceDraftFromPortrait(portrait, cassetteId)
      : emptyResourceDraft(cassetteId),
  [mode, portrait, cassetteId]);

  function saved(result: ResourceWriteResponse): void {
    setNotice(result.indexReady
      ? `Ревизия ${result.resourceId} сохранена.`
      : "Ревизия сохранена, но индекс требует восстановления.");
    setMode(null);
    const sameResource = portrait?.resourceId === result.resourceId;
    props.onSelect(result.resourceId);
    if (sameResource) props.onReload();
  }

  if (mode !== null) {
    return (
      <ResourceWorkspaceModePane
        mode={mode}
        initialDraft={initialDraft}
        writableCassettes={writable}
        documentCassettes={documentCassettes}
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
        canAddDocument={documentCassettes.length > 0}
        canEdit={writable.length > 0 && portrait !== null && portrait.type !== null}
        notice={notice}
        onCreate={() => { setNotice(null); setMode("create"); }}
        onAddDocument={() => { setNotice(null); setMode("document"); }}
        onEdit={() => { setNotice(null); setMode("edit"); }}
      />
      <ResourcePortraitView
        page={props.page}
        loading={props.loading}
        error={props.error}
        token={props.token}
        project={props.project}
      />
    </>
  );
}
