import type { ProjectOverview } from "../api/models";
import type { ResourceRouteMode } from "../app/routes";
import { useResourcePage } from "../app/useResourcePage";
import { ResourceWorkspace } from "./ResourceWorkspace";

interface ResourcePageProps {
  project: ProjectOverview | null;
  token: string;
  mode: ResourceRouteMode;
  resource: ReturnType<typeof useResourcePage>;
  onCreate: () => void;
  onSelect: (resourceId: string) => void;
  onModeChange: (mode: ResourceRouteMode, replace?: boolean) => void;
}

export function ResourcePage(props: ResourcePageProps) {
  return (
    <main className="page-shell resource-page-shell">
      <section className="panel resource-page-panel">
        <ResourceWorkspace
          mode={props.mode}
          page={props.resource.page}
          loading={props.resource.loading}
          error={props.resource.error}
          token={props.token}
          project={props.project}
          onCreate={props.onCreate}
          onSelect={props.onSelect}
          onModeChange={props.onModeChange}
          onReload={props.resource.reload}
        />
      </section>
    </main>
  );
}
