import { useMemo } from "react";
import { useSearch } from "../app/useSearch";
import { SearchPanel } from "./SearchPanel";
import { SearchResultList } from "./SearchResultList";
import {
  filterSearchResultsByType,
  SearchTypeFacets
} from "./SearchTypeFacets";

interface SearchPageProps {
  search: ReturnType<typeof useSearch>;
  selectedTypeId: string | null;
  onSearch: (query: string) => void;
  onTypeChange: (typeId: string | null) => void;
}

export function SearchPage({
  search,
  selectedTypeId,
  onSearch,
  onTypeChange
}: SearchPageProps) {
  const visibleResults = useMemo(
    () => filterSearchResultsByType(search.results, selectedTypeId),
    [search.results, selectedTypeId]
  );

  return (
    <main className="page-shell search-page-shell">
      <section className="panel search-page-panel">
        <SearchPanel
          query={search.query}
          loading={search.loading}
          error={search.error}
          onSearch={onSearch}
        />
        {search.results.length > 0 && (
          <SearchTypeFacets
            results={search.results}
            selectedTypeId={selectedTypeId}
            onChange={onTypeChange}
          />
        )}
        <SearchResultList
          query={search.query}
          loading={search.loading}
          results={visibleResults}
          totalResults={search.results.length}
          typeFiltered={selectedTypeId !== null}
        />
      </section>
    </main>
  );
}
