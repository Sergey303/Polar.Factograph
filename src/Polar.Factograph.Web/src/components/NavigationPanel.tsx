import type { ProjectOverview } from "../api/models";
import { ProjectScope } from "./ProjectScope";

interface NavigationPanelProps {
  project: ProjectOverview | null;
  loading: boolean;
  error: string | null;
}

export function NavigationPanel(props: NavigationPanelProps) {
  return (
    <aside className="panel navigation-panel">
      <ProjectScope
        project={props.project}
        loading={props.loading}
        error={props.error}
      />
    </aside>
  );
}
