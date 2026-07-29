import type { ResourceSearchResult } from "../api/models";
import { followAppLink, resourceHref } from "../app/routes";

interface SearchResultListProps {
  query: string;
  loading: boolean;
  results: ResourceSearchResult[];
}

export function SearchResultList({ query, loading, results }: SearchResultListProps) {
  if (results.length === 0) {
    const title = loading
      ? "Идёт поиск…"
      : query.length > 0
        ? "Подходящие сущности не найдены"
        : "Введите поисковый запрос";
    const description = query.length > 0
      ? "Попробуйте другое имя, название или слова из описания."
      : "Искомая строка сохранится в адресе страницы, поэтому результат можно открыть повторно или отправить ссылкой.";
    return (
      <div className="empty-state">
        <strong>{title}</strong>
        {!loading && <span>{description}</span>}
      </div>
    );
  }

  return (
    <ol className="result-list">
      {results.map(result => (
        <li key={result.resourceId}>
          <a href={resourceHref(result.resourceId)} onClick={followAppLink}>
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
