import { useSearch } from "../app/useSearch";
import { SearchPanel } from "./SearchPanel";
import { SearchResultList } from "./SearchResultList";

interface SearchPageProps {
  search: ReturnType<typeof useSearch>;
}

export function SearchPage({ search }: SearchPageProps) {
  return (
    <main className="page-shell search-page-shell">
      <section className="panel search-page-panel">
        <SearchPanel
          query={search.query}
          loading={search.loading}
          error={search.error}
          onQueryChange={search.setQuery}
          onSearch={search.search}
          onClear={search.clear}
        />
        <SearchResultList results={search.results} />
      </section>
    </main>
  );
}
