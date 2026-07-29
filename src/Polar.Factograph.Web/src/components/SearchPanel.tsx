import { type FormEvent, useEffect, useState } from "react";

interface SearchPanelProps {
  query: string;
  loading: boolean;
  error: string | null;
  onSearch: (query: string) => void;
}

export function SearchPanel(props: SearchPanelProps) {
  const [draft, setDraft] = useState(props.query);

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
    <div className="search-box">
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
        <button className="button primary" disabled={props.loading}>
          {props.loading ? "Ищем…" : "Найти"}
        </button>
        {draft.length > 0 && (
          <button className="button ghost" type="button" onClick={clear}>
            Сбросить
          </button>
        )}
      </form>
      {props.error && <div className="notice error">{props.error}</div>}
    </div>
  );
}
