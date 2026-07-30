import { useMemo } from "react";
import type { ResourceSearchResult } from "../api/models";

export const unknownSearchTypeId = "$unknown";

interface SearchTypeFacetsProps {
  results: ResourceSearchResult[];
  selectedTypeId: string | null;
  onChange: (typeId: string | null) => void;
}

interface TypeFacet {
  typeId: string;
  label: string;
  count: number;
}

export function filterSearchResultsByType(
  results: ResourceSearchResult[],
  selectedTypeId: string | null
): ResourceSearchResult[] {
  if (selectedTypeId === null) return results;
  return results.filter(result => selectedTypeId === unknownSearchTypeId
    ? result.type === null
    : result.type === selectedTypeId);
}

export function SearchTypeFacets(props: SearchTypeFacetsProps) {
  const facets = useMemo(() => buildFacets(props.results), [props.results]);
  if (facets.length === 0) return null;

  return (
    <nav className="search-type-facets" aria-label="Фильтр результатов по типу">
      <button
        className={props.selectedTypeId === null ? "active" : undefined}
        type="button"
        aria-pressed={props.selectedTypeId === null}
        onClick={() => props.onChange(null)}
      >
        <span>Все типы</span>
        <strong>{props.results.length}</strong>
      </button>
      {facets.map(facet => (
        <button
          className={props.selectedTypeId === facet.typeId ? "active" : undefined}
          type="button"
          key={facet.typeId}
          aria-pressed={props.selectedTypeId === facet.typeId}
          onClick={() => props.onChange(facet.typeId)}
        >
          <span>{facet.label}</span>
          <strong>{facet.count}</strong>
        </button>
      ))}
    </nav>
  );
}

function buildFacets(results: ResourceSearchResult[]): TypeFacet[] {
  const grouped = new Map<string, TypeFacet>();
  for (const result of results) {
    const typeId = result.type ?? unknownSearchTypeId;
    const label = result.typeLabel?.trim() || result.type || "Тип не указан";
    const existing = grouped.get(typeId);
    if (existing) {
      existing.count += 1;
    } else {
      grouped.set(typeId, { typeId, label, count: 1 });
    }
  }

  return [...grouped.values()].sort((left, right) =>
    right.count - left.count ||
    left.label.localeCompare(right.label, "ru") ||
    left.typeId.localeCompare(right.typeId, "ru"));
}
