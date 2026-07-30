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
  navigateToSearchClass,
  navigateToSearchFilter,
  type ResourceRouteMode,
  useAppRoute
} from "./routes";
import { useAuthentication } from "./useAuthentication";
import { useOntologyClassSearch } from "./useOntologyClassSearch";
import { useProject } from "./useProject";
import { useResourcePage } from "./useResourcePage";
import { useSearch } from "./useSearch";

export function App() {
  const auth = useAuthentication();

  if (auth.initializing || (!auth.authenticated && !auth.publicReadEnabled)) {
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

  return <ProjectWorkspace auth={auth} />;
}

interface ProjectWorkspaceProps {
  auth: ReturnType<typeof useAuthentication>;
}

function ProjectWorkspace({ auth }: ProjectWorkspaceProps) {
  const [adminOpen, setAdminOpen] = useState(false);
  const route = useAppRoute();
  const project = useProject(auth.token);
  const searchQuery = route.page === "search" ? route.query : null;
  const searchTypeId = route.page === "search" ? route.typeId : null;
  const searchClassId = route.page === "search" ? route.classId : null;
  const searchClassOffset = route.page === "search" ? route.classOffset : 0;
  const search = useSearch(searchQuery, auth.token);
  const classSearch = useOntologyClassSearch(
    searchQuery,
    searchClassId,
    searchClassOffset,
    auth.token
  );
  const resourceId = route.page === "resource" ? route.resourceId : null;
  const resourceMode: ResourceRouteMode = route.page === "resource"
    ? route.mode
    : "view";
  const resource = useResourcePage(resourceId, auth.token);
  const canonicalResourceId = resource.page?.portrait.resourceId ?? null;
  const canAdmin = project.project?.canAdmin ?? false;
  const pageLoading = route.page === "search"
    ? searchClassId === null
      ? search.loading
      : classSearch.pageLoading
    : route.page === "resource"
      ? resource.refreshing
      : false;

  useEffect(() => {
    setAdminOpen(false);
  }, [auth.token]);

  useEffect(() => {
    if (!canAdmin) setAdminOpen(false);
  }, [canAdmin]);

  useEffect(() => {
    if (
      resourceId !== null &&
      canonicalResourceId !== null &&
      canonicalResourceId !== resourceId
    ) {
      navigateToResourceMode(canonicalResourceId, resourceMode, true);
    }
  }, [resourceId, resourceMode, canonicalResourceId]);

  function submitSearch(query: string): void {
    const normalized = query.trim();
    if (
      route.page === "search" &&
      normalized === route.query &&
      route.typeId === null &&
      route.classId === null
    ) {
      search.reload();
      classSearch.reload();
      return;
    }
    navigateToSearch(normalized);
  }

  function changeSearchType(typeId: string | null): void {
    if (route.page === "search" && route.typeId !== typeId) {
      navigateToSearchFilter(route.query, typeId);
    }
  }

  function openSearchClass(classId: string): void {
    if (route.page === "search") {
      navigateToSearchClass(route.query, classId);
    }
  }

  function closeSearchClass(): void {
    if (route.page === "search") {
      navigateToSearch(route.query);
    }
  }

  function changeSearchClassOffset(offset: number): void {
    if (route.page === "search" && route.classId !== null) {
      navigateToSearchClass(route.query, route.classId, offset);
    }
  }

  return (
    <div className="app-shell">
      <TopBar
        project={project.project}
        loading={project.loading || pageLoading}
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
          if (route.page === "search") {
            search.reload();
            classSearch.reload();
          }
          if (route.page === "resource") resource.reload();
        }}
        onAdmin={() => setAdminOpen(true)}
      />

      {route.page === "search" && (
        <SearchPage
          search={search}
          classSearch={classSearch}
          selectedTypeId={searchTypeId}
          selectedClassId={searchClassId}
          onSearch={submitSearch}
          onTypeChange={changeSearchType}
          onClassSelect={openSearchClass}
          onClassBack={closeSearchClass}
          onClassOffsetChange={changeSearchClassOffset}
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
