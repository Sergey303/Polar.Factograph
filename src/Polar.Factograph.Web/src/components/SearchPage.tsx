import { useSearch } from "../app/useSearch";
import { SearchPanel } from "./SearchPanel";
import { SearchResultList } from "./SearchResultList";

interface SearchPageProps {
  search: ReturnType<typeof useSearch>;
  onSearch: (query: string) => void;
}

export function SearchPage({ search, onSearch }: SearchPageProps) {
  return (
    <main className="page-shell search-page-shell">
      <section className="panel search-page-panel">
        <SearchPanel
          query={search.query}
          loading={search.loading}
          error={search.error}
          onSearch={onSearch}
        />
        <SearchResultList
          query={search.query}
          loading={search.loading}
          results={search.results}
        />
      </section>
    </main>
  );
}
