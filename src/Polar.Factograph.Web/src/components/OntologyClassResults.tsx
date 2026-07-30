import type { ResourceTypeSearchPage } from "../api/models";
import { SearchResultList } from "./SearchResultList";

interface OntologyClassResultsProps {
  page: ResourceTypeSearchPage | null;
  loading: boolean;
  error: string | null;
  onBack: () => void;
  onOffsetChange: (offset: number) => void;
}

export function OntologyClassResults(props: OntologyClassResultsProps) {
  if (props.error !== null) {
    return <div className="notice error ontology-class-error">{props.error}</div>;
  }
  if (props.page === null) {
    return (
      <div className="empty-state ontology-class-loading">
        <strong>{props.loading ? "Загружаем категорию…" : "Категория не загружена"}</strong>
      </div>
    );
  }

  const page = props.page;
  const from = page.results.length === 0 ? 0 : page.offset + 1;
  const to = page.offset + page.results.length;
  const previousOffset = Math.max(0, page.offset - page.limit);
  const nextOffset = page.offset + page.limit;

  return (
    <section className="ontology-class-results">
      <header>
        <div>
          <span className="eyebrow">Категория</span>
          <h1>{page.label}</h1>
          <span className="muted">Всего: {page.total}</span>
        </div>
        <button className="button ghost compact" type="button" onClick={props.onBack}>
          К результатам запроса
        </button>
      </header>

      <SearchResultList
        query={page.label}
        loading={props.loading}
        results={page.results}
        totalResults={page.total}
        typeFiltered={false}
      />

      {page.total > page.limit && (
        <nav className="ontology-class-pagination" aria-label={`Страницы категории «${page.label}»`}>
          <button
            className="button ghost compact"
            type="button"
            disabled={page.offset === 0 || props.loading}
            onClick={() => props.onOffsetChange(previousOffset)}
          >
            Предыдущие
          </button>
          <span>{from}–{to} из {page.total}</span>
          <button
            className="button ghost compact"
            type="button"
            disabled={nextOffset >= page.total || props.loading}
            onClick={() => props.onOffsetChange(nextOffset)}
          >
            Следующие
          </button>
        </nav>
      )}
    </section>
  );
}
