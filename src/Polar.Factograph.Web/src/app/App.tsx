import { useEffect, useState } from "react";
import { AdminDialog } from "../components/AdminDialog";
import { AuthenticationPage } from "../components/AuthenticationPage";
import { NavigationPanel } from "../components/NavigationPanel";
import { ResourceWorkspace } from "../components/ResourceWorkspace";
import { SearchPanel } from "../components/SearchPanel";
import { SearchResultList } from "../components/SearchResultList";
import { TopBar } from "../components/TopBar";
import { useAuthentication } from "./useAuthentication";
import { usePortrait } from "./usePortrait";
import { useProject } from "./useProject";
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
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [adminOpen, setAdminOpen] = useState(false);
  const project = useProject(auth.token);
  const search = useSearch(auth.token);
  const portrait = usePortrait(selectedId, auth.token);
  const canAdmin = project.project?.projectRights.includes("rebuildIndex") ?? false;

  useEffect(() => {
    setSelectedId(null);
    setAdminOpen(false);
    search.clear();
  }, [auth.token]);

  useEffect(() => {
    if (!canAdmin) setAdminOpen(false);
  }, [canAdmin]);

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
        onReload={project.reload}
        onAdmin={() => setAdminOpen(true)}
      />

      <main className="workspace">
        <NavigationPanel
          project={project.project}
          loading={project.loading}
          error={project.error}
          token={auth.token}
          selectedResourceId={selectedId}
          onSelect={setSelectedId}
        />

        <section className="panel results-panel">
          <SearchPanel
            mode={search.mode}
            query={search.query}
            loading={search.loading}
            error={search.error}
            onModeChange={search.setMode}
            onQueryChange={search.setQuery}
            onSearch={search.search}
            onClear={() => {
              search.clear();
              setSelectedId(null);
            }}
          />
          <SearchResultList
            results={search.results}
            selectedId={selectedId}
            onSelect={setSelectedId}
          />
        </section>

        <section className="panel portrait-panel">
          <ResourceWorkspace
            portrait={portrait.portrait}
            loading={portrait.loading}
            error={portrait.error}
            token={auth.token}
            project={project.project}
            onSelect={setSelectedId}
            onReload={portrait.reload}
          />
        </section>
      </main>

      {adminOpen && canAdmin && (
        <AdminDialog token={auth.token} onClose={() => setAdminOpen(false)} />
      )}
    </div>
  );
}
