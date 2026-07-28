import type { ProjectOverview } from "../api/models";
import { searchHref } from "../app/routes";
import { useResourcePage } from "../app/useResourcePage";
import { ResourceWorkspace } from "./ResourceWorkspace";

interface ResourcePageProps {
  project: ProjectOverview | null;
  token: string;
  resource: ReturnType<typeof useResourcePage>;
  onCreate: () => void;
  onSelect: (resourceId: string) => void;
}

export function ResourcePage(props: ResourcePageProps) {
  return (
    <main className="page-shell resource-page-shell">
      <nav className="page-navigation" aria-label="Навигация по проекту">
        <a className="button ghost" href={searchHref}>← К поиску</a>
      </nav>
      <section className="panel resource-page-panel">
        <ResourceWorkspace
          page={props.resource.page}
          loading={props.resource.loading}
          error={props.resource.error}
          token={props.token}
          project={props.project}
          onCreate={props.onCreate}
          onSelect={props.onSelect}
          onReload={props.resource.reload}
        />
      </section>
    </main>
  );
}
