import { useState } from "react";
import { readAccessToken, writeAccessToken } from "../api/tokenStore";
import { NavigationPanel } from "../components/NavigationPanel";
import { ResourcePortraitView } from "../components/ResourcePortraitView";
import { SearchPanel } from "../components/SearchPanel";
import { SearchResultList } from "../components/SearchResultList";
import { TopBar } from "../components/TopBar";
import { usePortrait } from "./usePortrait";
import { useProject } from "./useProject";
import { useSearch } from "./useSearch";

export function App() {
  const [token, setToken] = useState(readAccessToken);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const project = useProject(token);
  const search = useSearch(token);
  const portrait = usePortrait(selectedId, token);

  function saveToken(value: string): void {
    writeAccessToken(value);
    setToken(value.trim());
    setSelectedId(null);
    search.clear();
  }

  return (
    <div className="app-shell">
      <TopBar
        project={project.project}
        token={token}
        loading={project.loading}
        onTokenSave={saveToken}
        onReload={project.reload}
      />

      <main className="workspace">
        <NavigationPanel
          project={project.project}
          loading={project.loading}
          error={project.error}
          token={token}
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
          <ResourcePortraitView
            portrait={portrait.portrait}
            loading={portrait.loading}
            error={portrait.error}
            token={token}
            project={project.project}
            onSelect={setSelectedId}
          />
        </section>
      </main>
    </div>
  );
}
