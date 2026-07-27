import type { FormEvent } from "react";

interface SearchPanelProps {
  query: string;
  loading: boolean;
  error: string | null;
  onQueryChange: (query: string) => void;
  onSearch: () => void;
  onClear: () => void;
}

export function SearchPanel(props: SearchPanelProps) {
  function submit(event: FormEvent): void {
    event.preventDefault();
    props.onSearch();
  }

  return (
    <div className="search-box">
      <div className="panel-heading compact">
        <span className="eyebrow">Поиск</span>
        <h2>Ресурсы проекта</h2>
      </div>
      <form className="search-form" onSubmit={submit}>
        <input
          value={props.query}
          onChange={event => props.onQueryChange(event.target.value)}
          placeholder="Имя, название или слова из описания"
          aria-label="Поиск по ресурсам проекта"
        />
        <button className="button primary" disabled={props.loading}>
          {props.loading ? "Ищем…" : "Найти"}
        </button>
        {props.query && (
          <button className="button ghost" type="button" onClick={props.onClear}>
            Сбросить
          </button>
        )}
      </form>
      {props.error && <div className="notice error">{props.error}</div>}
    </div>
  );
}
