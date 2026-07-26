import type { ProjectOverview } from "../api/models";
import { CollectionWorkspace } from "./CollectionWorkspace";
import { ProjectScope } from "./ProjectScope";

interface NavigationPanelProps {
  project: ProjectOverview | null;
  loading: boolean;
  error: string | null;
  token: string;
  selectedResourceId: string | null;
  onSelect: (resourceId: string) => void;
}

export function NavigationPanel(props: NavigationPanelProps) {
  return (
    <aside className="panel navigation-panel">
      <ProjectScope
        project={props.project}
        loading={props.loading}
        error={props.error}
      />
      <CollectionWorkspace
        project={props.project}
        token={props.token}
        selectedResourceId={props.selectedResourceId}
        onSelect={props.onSelect}
      />
    </aside>
  );
}
