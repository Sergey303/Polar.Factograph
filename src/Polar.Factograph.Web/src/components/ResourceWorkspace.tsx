import { useEffect, useMemo, useState } from "react";
import type { ProjectOverview, SemanticResourcePage } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { cassettesWithRight, cassettesWithRights } from "../app/projectAccess";
import {
  emptyResourceDraft,
  resourceDraftFromPortrait
} from "../app/resourceDraftFactory";
import { resourceDocumentUris } from "../app/resourceDocuments";
import { preferredResourceCassette } from "../app/resourceEditorCassette";
import type { ResourceRouteMode } from "../app/routes";
import { ComplexRelationCreatePane } from "./ComplexRelationCreatePane";
import { DocumentSection } from "./DocumentSection";
import { ResourcePortraitView } from "./ResourcePortraitView";
import { ResourceWorkspaceActions } from "./ResourceWorkspaceActions";
import { ResourceWorkspaceModePane } from "./ResourceWorkspaceModePane";

const photoDocumentType = "http://fogid.net/o/photo-doc";

interface ResourceWorkspaceProps {
  project: ProjectOverview | null;
  page: SemanticResourcePage | null;
  loading: boolean;
  error: string | null;
  token: string;
  mode: ResourceRouteMode;
  onCreate: () => void;
  onSelect: (resourceId: string) => void;
  onModeChange: (mode: ResourceRouteMode, replace?: boolean) => void;
  onReload: () => void;
}

export function ResourceWorkspace(props: ResourceWorkspaceProps) {
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
    props.mode === "edit" || props.mode === "relations" ? portrait : null,
    writable
  );
  const initialDraft = useMemo(() =>
    props.mode === "edit" && portrait !== null
      ? resourceDraftFromPortrait(portrait, cassetteId)
      : emptyResourceDraft(cassetteId),
  [props.mode, portrait, cassetteId]);
  const modeDenied = props.project !== null && (
    (props.mode === "edit" || props.mode === "relations") && writable.length === 0 ||
    props.mode === "document" && documentCassettes.length === 0
  );

  useEffect(() => {
    setNotice(null);
  }, [portrait?.resourceId]);

  useEffect(() => {
    if (modeDenied) {
      props.onModeChange("view", true);
    }
  }, [modeDenied, props.onModeChange]);

  function saved(result: ResourceWriteResponse): void {
    setNotice(result.indexReady
      ? `Ревизия ${result.resourceId} сохранена.`
      : "Ревизия сохранена, но индекс требует восстановления.");
    const sameResource = portrait?.resourceId === result.resourceId;
    if (sameResource) {
      props.onModeChange("view", true);
      props.onReload();
    } else {
      props.onSelect(result.resourceId);
    }
  }

  function relationSaved(result: ResourceWriteResponse): void {
    setNotice(result.indexReady
      ? "Связь сохранена."
      : "Связь сохранена, но индекс требует восстановления.");
    props.onModeChange("view", true);
    props.onReload();
  }

  if (modeDenied) {
    return (
      <ResourcePortraitView
        page={props.page}
        loading={props.loading}
        error={props.error}
        token={props.token}
        project={props.project}
      />
    );
  }

  if ((props.mode === "edit" || props.mode === "relations") && portrait === null) {
    return (
      <ResourcePortraitView
        page={props.page}
        loading={props.loading}
        error={props.error}
        token={props.token}
        project={props.project}
      />
    );
  }

  if (props.mode === "relations" && portrait !== null) {
    return (
      <ComplexRelationCreatePane
        portrait={portrait}
        cassettes={writable}
        cassetteId={cassetteId}
        token={props.token}
        onCancel={() => props.onModeChange("view", true)}
        onSaved={relationSaved}
      />
    );
  }

  if (props.mode === "edit" || props.mode === "document") {
    const editingPhoto = props.mode === "edit" && portrait?.type === photoDocumentType;
    return (
      <>
        <ResourceWorkspaceModePane
          mode={props.mode}
          initialDraft={initialDraft}
          writableCassettes={writable}
          documentCassettes={documentCassettes}
          token={props.token}
          onCancel={() => props.onModeChange("view", true)}
          onSaved={saved}
        />
        {editingPhoto && portrait !== null && (
          <DocumentSection
            uris={resourceDocumentUris(portrait)}
            token={props.token}
            project={props.project}
            title="Изображение"
            previewPolicy="largest-preview"
            imageDocument
            allowReplace
          />
        )}
      </>
    );
  }

  const canWriteEntity = writable.length > 0 && portrait !== null && portrait.type !== null;
  return (
    <>
      <ResourceWorkspaceActions
        canCreate={writable.length > 0}
        canAddDocument={documentCassettes.length > 0}
        canAddRelation={canWriteEntity}
        canEdit={canWriteEntity}
        notice={notice}
        onCreate={() => { setNotice(null); props.onCreate(); }}
        onAddDocument={() => { setNotice(null); props.onModeChange("document"); }}
        onAddRelation={() => { setNotice(null); props.onModeChange("relations"); }}
        onEdit={() => { setNotice(null); props.onModeChange("edit"); }}
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
