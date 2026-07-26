import type { ResourceSearchResult } from "../api/models";

interface SearchResultListProps {
  results: ResourceSearchResult[];
  selectedId: string | null;
  onSelect: (resourceId: string) => void;
}

export function SearchResultList({
  results,
  selectedId,
  onSelect
}: SearchResultListProps) {
  if (results.length === 0) {
    return (
      <div className="empty-state">
        <strong>Результатов пока нет</strong>
        <span>Введите запрос или измените режим поиска.</span>
      </div>
    );
  }

  return (
    <ol className="result-list">
      {results.map(result => (
        <li key={result.resourceId}>
          <button
            className={result.resourceId === selectedId ? "selected" : ""}
            onClick={() => onSelect(result.resourceId)}
          >
            <span className="result-title">{result.displayName}</span>
            <span className="result-meta">
              {result.typeLabel ?? result.type ?? "Тип не указан"}
              <span>·</span>
              {result.sourceCassetteId}
            </span>
            {result.matches[0] && (
              <span className="result-evidence">
                {result.matches[0].value}
              </span>
            )}
          </button>
        </li>
      ))}
    </ol>
  );
}
