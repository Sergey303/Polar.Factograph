import type { ReactNode } from "react";
import type { ResourceSearchResult } from "../api/models";
import { followAppLink, resourceHref } from "../app/routes";

interface SearchResultListProps {
  query: string;
  loading: boolean;
  results: ResourceSearchResult[];
  totalResults: number;
  typeFiltered: boolean;
  afterFirst?: ReactNode;
}

export function SearchResultList({
  query,
  loading,
  results,
  totalResults,
  typeFiltered,
  afterFirst
}: SearchResultListProps) {
  if (results.length === 0) {
    const filteredEmpty = !loading && typeFiltered && totalResults > 0;
    const title = loading
      ? "Идёт поиск…"
      : filteredEmpty
        ? "В выбранном типе результатов нет"
        : query.length > 0
          ? "Подходящие сущности не найдены"
          : "Введите поисковый запрос";
    const description = filteredEmpty
      ? "Выберите другой тип или вернитесь ко всем результатам."
      : query.length > 0
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
      {results.map((result, index) => (
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
          {index === 0 && afterFirst}
        </li>
      ))}
    </ol>
  );
}
