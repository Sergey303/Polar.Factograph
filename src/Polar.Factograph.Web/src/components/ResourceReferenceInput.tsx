import { useRef, useState } from "react";
import type {
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";
import type { ResourceSearchResult } from "../api/models";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import { ontologyTypeMatchesRanges } from "../app/ontologyRelations";

interface ResourceReferenceInputProps {
  property: OntologyWriteProperty;
  schema: OntologyWriteSchema;
  token: string;
  value: string;
  readOnly?: boolean;
  onChange: (value: string) => void;
  onCreateNew?: (
    property: OntologyWriteProperty,
    onCreated: (resourceId: string) => void
  ) => void;
}

export function ResourceReferenceInput(props: ResourceReferenceInputProps) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<ResourceSearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const activeRequest = useRef<AbortController | null>(null);

  async function search(): Promise<void> {
    const text = query.trim();
    activeRequest.current?.abort();
    if (text.length === 0) {
      setResults([]);
      setError(null);
      return;
    }

    const controller = new AbortController();
    activeRequest.current = controller;
    setLoading(true);
    setError(null);
    try {
      const found = await factographApi.search(text, props.token, controller.signal);
      setResults(found.filter(result =>
        ontologyTypeMatchesRanges(
          props.schema,
          result.type,
          props.property.ranges
        ) && props.schema.classes.some(type =>
          type.id === result.type && type.isEntityType && !type.isAbstract
        )));
    } catch (reason) {
      if (!controller.signal.aborted) {
        setResults([]);
        setError(errorText(reason));
      }
    } finally {
      if (activeRequest.current === controller) {
        activeRequest.current = null;
        setLoading(false);
      }
    }
  }

  if (props.readOnly) {
    return <input value={props.value} readOnly />;
  }

  return (
    <div className="resource-reference-input">
      <label>
        <span>Выбранная сущность</span>
        <input
          value={props.value}
          onChange={event => props.onChange(event.target.value)}
          placeholder="Идентификатор сущности"
        />
      </label>
      <div className="resource-reference-search">
        <input
          value={query}
          onChange={event => setQuery(event.target.value)}
          onKeyDown={event => {
            if (event.key === "Enter") {
              event.preventDefault();
              void search();
            }
          }}
          placeholder="Найдите сущность по имени"
        />
        <button className="button subtle compact" type="button" onClick={() => void search()} disabled={loading}>
          {loading ? "Поиск…" : "Найти"}
        </button>
        {props.onCreateNew && (
          <button
            className="button subtle compact"
            type="button"
            onClick={() => props.onCreateNew?.(props.property, props.onChange)}
          >
            Создать новую сущность
          </button>
        )}
      </div>
      {error && <span className="notice error">{error}</span>}
      {!loading && query.trim().length > 0 && results.length === 0 && !error && (
        <span className="muted">Подходящие сущности не найдены.</span>
      )}
      {results.length > 0 && (
        <div className="resource-reference-results">
          {results.map(result => (
            <button
              key={result.resourceId}
              className="relation-row"
              type="button"
              onClick={() => {
                props.onChange(result.resourceId);
                setResults([]);
                setQuery(result.displayName);
              }}
            >
              <span>{result.typeLabel ?? "Сущность"}</span>
              <strong>{result.displayName}</strong>
              <span className="muted mono">{result.resourceId}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
