import { type FormEvent, useEffect, useState } from "react";

interface SearchPanelProps {
  query: string;
  loading: boolean;
  error: string | null;
  onSearch: (query: string) => void;
}

export function SearchPanel(props: SearchPanelProps) {
  const [draft, setDraft] = useState(props.query);
  const clearHidden = draft.length === 0;

  useEffect(() => {
    setDraft(props.query);
  }, [props.query]);

  function submit(event: FormEvent): void {
    event.preventDefault();
    props.onSearch(draft);
  }

  function clear(): void {
    setDraft("");
    props.onSearch("");
  }

  return (
    <div className="search-box" aria-busy={props.loading}>
      <div className="panel-heading compact">
        <span className="eyebrow">Поиск</span>
        <h2>Ресурсы проекта</h2>
      </div>
      <form className="search-form" onSubmit={submit}>
        <input
          value={draft}
          onChange={event => setDraft(event.target.value)}
          placeholder="Имя, название или слова из описания"
          aria-label="Поиск по ресурсам проекта"
        />
        <button
          className="button primary search-submit-button"
          type="submit"
          disabled={props.loading}
        >
          {props.loading && <span className="button-spinner" aria-hidden="true" />}
          <span>{props.loading ? "Ищем…" : "Найти"}</span>
        </button>
        <button
          className={`button ghost search-clear-button${clearHidden ? " is-placeholder" : ""}`}
          type="button"
          disabled={props.loading || clearHidden}
          aria-hidden={clearHidden}
          tabIndex={clearHidden ? -1 : 0}
          onClick={clear}
        >
          Сбросить
        </button>
      </form>
      <div
        className={`search-progress${props.loading ? " is-active" : ""}`}
        aria-label={props.loading ? "Идёт поиск" : undefined}
        aria-hidden={!props.loading}
      >
        <span />
      </div>
      {props.error && <div className="notice error">{props.error}</div>}
    </div>
  );
}
