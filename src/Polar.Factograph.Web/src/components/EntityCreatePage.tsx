import { useMemo } from "react";
import type { ProjectOverview } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { cassettesWithRight } from "../app/projectAccess";
import { emptyResourceDraft } from "../app/resourceDraftFactory";
import { preferredResourceCassette } from "../app/resourceEditorCassette";
import { navigateToResource, navigateToSearch, searchHref } from "../app/routes";
import { ResourceEditorPane } from "./ResourceEditorPane";

interface EntityCreatePageProps {
  project: ProjectOverview | null;
  token: string;
}

export function EntityCreatePage(props: EntityCreatePageProps) {
  const writable = useMemo(
    () => cassettesWithRight(props.project, "writeMetadata"),
    [props.project]
  );
  const cassetteId = preferredResourceCassette(props.project, null, writable);
  const initialDraft = useMemo(
    () => emptyResourceDraft(cassetteId),
    [cassetteId]
  );

  function saved(result: ResourceWriteResponse): void {
    navigateToResource(result.resourceId, true);
  }

  return (
    <main className="page-shell resource-page-shell">
      <nav className="page-navigation" aria-label="Навигация по проекту">
        <a className="button ghost" href={searchHref}>← К поиску</a>
      </nav>
      <section className="panel resource-page-panel">
        <ResourceEditorPane
          mode="create"
          initialDraft={initialDraft}
          cassettes={writable}
          token={props.token}
          onCancel={() => navigateToSearch()}
          onSaved={saved}
        />
      </section>
    </main>
  );
}
