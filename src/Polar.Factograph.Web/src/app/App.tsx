import { useEffect, useState } from "react";
import { AdminDialog } from "../components/AdminDialog";
import { AuthenticationPage } from "../components/AuthenticationPage";
import { EntityCreatePage } from "../components/EntityCreatePage";
import { ResourcePage } from "../components/ResourcePage";
import { SearchPage } from "../components/SearchPage";
import { TopBar } from "../components/TopBar";
import {
  navigateToCreateEntity,
  navigateToResource,
  navigateToResourceMode,
  navigateToSearch,
  useAppRoute
} from "./routes";
import { useAuthentication } from "./useAuthentication";
import { useProject } from "./useProject";
import { useResourcePage } from "./useResourcePage";
import { useSearch } from "./useSearch";

export function App() {
  const auth = useAuthentication();

  if (auth.initializing || !auth.authenticated) {
    return (
      <AuthenticationPage
        initializing={auth.initializing}
        registrationEnabled={auth.registrationEnabled}
        user={auth.user}
        busy={auth.busy}
        error={auth.error}
        onLogin={auth.login}
        onRegister={auth.register}
        onLogout={auth.logout}
      />
    );
  }

  return <AuthenticatedWorkspace auth={auth} />;
}

interface AuthenticatedWorkspaceProps {
  auth: ReturnType<typeof useAuthentication>;
}

function AuthenticatedWorkspace({ auth }: AuthenticatedWorkspaceProps) {
  const [adminOpen, setAdminOpen] = useState(false);
  const route = useAppRoute();
  const project = useProject(auth.token);
  const searchQuery = route.page === "search" ? route.query : null;
  const search = useSearch(searchQuery, auth.token);
  const resourceId = route.page === "resource" ? route.resourceId : null;
  const resource = useResourcePage(resourceId, auth.token);
  const canAdmin = project.project?.projectRights.includes("rebuildIndex") ?? false;

  useEffect(() => {
    setAdminOpen(false);
  }, [auth.token]);

  useEffect(() => {
    if (!canAdmin) setAdminOpen(false);
  }, [canAdmin]);

  useEffect(() => {
    const canonicalId = resource.page?.portrait.resourceId;
    if (route.page === "resource" && canonicalId && canonicalId !== route.resourceId) {
      navigateToResourceMode(canonicalId, route.mode, true);
    }
  }, [route, resource.page?.portrait.resourceId]);

  return (
    <div className="app-shell">
      <TopBar
        project={project.project}
        loading={project.loading}
        canAdmin={canAdmin}
        authentication={{
          authenticated: auth.authenticated,
          registrationEnabled: auth.registrationEnabled,
          user: auth.user,
          initializing: auth.initializing,
          busy: auth.busy,
          error: auth.error,
          onLogin: auth.login,
          onRegister: auth.register,
          onLogout: auth.logout
        }}
        onReload={() => {
          project.reload();
          if (route.page === "search") search.reload();
          if (route.page === "resource") resource.reload();
        }}
        onAdmin={() => setAdminOpen(true)}
      />

      {route.page === "search" && (
        <SearchPage
          search={search}
          onSearch={query => navigateToSearch(query)}
        />
      )}
      {route.page === "create-entity" && (
        <EntityCreatePage project={project.project} token={auth.token} />
      )}
      {route.page === "resource" && (
        <ResourcePage
          project={project.project}
          token={auth.token}
          mode={route.mode}
          resource={resource}
          onCreate={navigateToCreateEntity}
          onSelect={selectedId => navigateToResource(selectedId)}
          onModeChange={(mode, replace = false) =>
            navigateToResourceMode(route.resourceId, mode, replace)}
        />
      )}

      {adminOpen && canAdmin && (
        <AdminDialog token={auth.token} onClose={() => setAdminOpen(false)} />
      )}
    </div>
  );
}
