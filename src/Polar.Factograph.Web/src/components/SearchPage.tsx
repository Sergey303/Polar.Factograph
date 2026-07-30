import { useMemo } from "react";
import { useOntologyClassSearch } from "../app/useOntologyClassSearch";
import { useSearch } from "../app/useSearch";
import { OntologyClassResults } from "./OntologyClassResults";
import { OntologyClassSuggestions } from "./OntologyClassSuggestions";
import { SearchPanel } from "./SearchPanel";
import { SearchResultList } from "./SearchResultList";
import {
  filterSearchResultsByType,
  SearchTypeFacets
} from "./SearchTypeFacets";

interface SearchPageProps {
  search: ReturnType<typeof useSearch>;
  classSearch: ReturnType<typeof useOntologyClassSearch>;
  selectedTypeId: string | null;
  selectedClassId: string | null;
  onSearch: (query: string) => void;
  onTypeChange: (typeId: string | null) => void;
  onClassSelect: (classId: string) => void;
  onClassBack: () => void;
  onClassOffsetChange: (offset: number) => void;
}

export function SearchPage({
  search,
  classSearch,
  selectedTypeId,
  selectedClassId,
  onSearch,
  onTypeChange,
  onClassSelect,
  onClassBack,
  onClassOffsetChange
}: SearchPageProps) {
  const visibleResults = useMemo(
    () => filterSearchResultsByType(search.results, selectedTypeId),
    [search.results, selectedTypeId]
  );
  const categorySuggestions = selectedTypeId === null ? (
    <OntologyClassSuggestions
      suggestions={classSearch.suggestions}
      loading={classSearch.suggestionsLoading}
      error={classSearch.suggestionsError}
      onSelect={onClassSelect}
    />
  ) : null;
  const classActive = selectedClassId !== null;

  return (
    <main className="page-shell search-page-shell">
      <section className="panel search-page-panel">
        <SearchPanel
          query={search.query}
          loading={search.loading || classSearch.pageLoading}
          error={search.error}
          onSearch={onSearch}
        />

        {classActive ? (
          <OntologyClassResults
            page={classSearch.page}
            loading={classSearch.pageLoading}
            error={classSearch.pageError}
            onBack={onClassBack}
            onOffsetChange={onClassOffsetChange}
          />
        ) : (
          <>
            {search.results.length > 0 && (
              <SearchTypeFacets
                results={search.results}
                selectedTypeId={selectedTypeId}
                onChange={onTypeChange}
              />
            )}
            {visibleResults.length === 0 && categorySuggestions}
            <SearchResultList
              query={search.query}
              loading={search.loading}
              results={visibleResults}
              totalResults={search.results.length}
              typeFiltered={selectedTypeId !== null}
              afterFirst={categorySuggestions}
            />
          </>
        )}
      </section>
    </main>
  );
}
