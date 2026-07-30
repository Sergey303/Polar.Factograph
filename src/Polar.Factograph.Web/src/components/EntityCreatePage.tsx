import { useMemo } from "react";
import type { ProjectOverview } from "../api/models";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { cassettesWithRight } from "../app/projectAccess";
import { emptyResourceDraft } from "../app/resourceDraftFactory";
import { preferredResourceCassette } from "../app/resourceEditorCassette";
import {
  followAppLink,
  navigateToResource,
  navigateToSearch,
  searchHref
} from "../app/routes";
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

  if (props.project === null) {
    return (
      <main className="page-shell resource-page-shell">
        <section className="panel resource-page-panel">
          <div className="empty-state"><strong>Загрузка проекта…</strong></div>
        </section>
      </main>
    );
  }

  if (writable.length === 0) {
    return (
      <main className="page-shell resource-page-shell">
        <section className="panel resource-page-panel">
          <div className="empty-state">
            <strong>Создание сущностей доступно только редакторам</strong>
            <span>Публичный режим позволяет просматривать и искать материалы без изменения данных.</span>
            <a className="button primary" href={searchHref} onClick={followAppLink}>
              Перейти к поиску
            </a>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className="page-shell resource-page-shell">
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
