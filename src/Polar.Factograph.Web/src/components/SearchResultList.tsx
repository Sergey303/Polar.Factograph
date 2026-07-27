import type { ResourceSearchResult } from "../api/models";
import { resourceHref } from "../app/routes";

interface SearchResultListProps {
  results: ResourceSearchResult[];
}

export function SearchResultList({ results }: SearchResultListProps) {
  if (results.length === 0) {
    return (
      <div className="empty-state">
        <strong>Результатов пока нет</strong>
        <span>Введите имя, название или слова из описания.</span>
      </div>
    );
  }

  return (
    <ol className="result-list">
      {results.map(result => (
        <li key={result.resourceId}>
          <a href={resourceHref(result.resourceId)}>
            <span className="result-title">{result.displayName}</span>
            <span className="result-meta">
              {result.typeLabel ?? result.type ?? "Тип не указан"}
            </span>
            {result.matches[0] && (
              <span className="result-evidence">
                {result.matches[0].value}
              </span>
            )}
          </a>
        </li>
      ))}
    </ol>
  );
}
