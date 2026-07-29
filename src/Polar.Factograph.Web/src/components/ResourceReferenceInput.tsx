import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import type {
  OntologyWriteProperty,
  OntologyWriteSchema
} from "../api/ontologyModels";
import { errorText } from "../api/errorText";
import {
  entityTypesMatchingRanges,
  ontologyTypeMatchesRanges
} from "../app/ontologyRelations";
import { searchQueryOptions } from "../app/queryOptions";

interface ResourceReferenceInputProps {
  property: OntologyWriteProperty;
  schema: OntologyWriteSchema;
  token: string;
  value: string;
  readOnly?: boolean;
  onChange: (value: string) => void;
  onCreateNew?: (
    property: OntologyWriteProperty,
    onCreated: (resourceId: string) => void,
    initialValue?: string
  ) => void;
}

export function ResourceReferenceInput(props: ResourceReferenceInputProps) {
  const [query, setQuery] = useState("");
  const [submittedQuery, setSubmittedQuery] = useState("");
  const [pendingCreateQuery, setPendingCreateQuery] = useState<string | null>(null);
  const [showDuplicateWarning, setShowDuplicateWarning] = useState(false);
  const search = useQuery({
    ...searchQueryOptions(submittedQuery, props.token),
    enabled: submittedQuery.length > 0
  });
  const results = useMemo(
    () => (search.data ?? []).filter(result =>
      ontologyTypeMatchesRanges(
        props.schema,
        result.type,
        props.property.ranges
      ) && props.schema.classes.some(type =>
        type.id === result.type && type.isEntityType && !type.isAbstract
      )),
    [search.data, props.schema, props.property.ranges]
  );
  const error = search.error === null ? null : errorText(search.error);
  const canCreateTarget = entityTypesMatchingRanges(
    props.schema,
    props.property.ranges
  ).length > 0;

  useEffect(() => {
    if (
      pendingCreateQuery === null ||
      submittedQuery !== pendingCreateQuery ||
      search.isFetching ||
      !search.isFetched
    ) {
      return;
    }

    const createValue = pendingCreateQuery;
    if (search.error !== null) {
      setPendingCreateQuery(null);
      return;
    }

    setPendingCreateQuery(null);
    if (results.length > 0) {
      setShowDuplicateWarning(true);
      return;
    }

    props.onCreateNew?.(props.property, props.onChange, createValue);
  }, [
    pendingCreateQuery,
    props.onChange,
    props.onCreateNew,
    props.property,
    results,
    search.error,
    search.isFetched,
    search.isFetching,
    submittedQuery
  ]);

  function submitSearch(): void {
    const normalized = query.trim();
    setShowDuplicateWarning(false);
    setPendingCreateQuery(null);
    setSubmittedQuery(normalized);
  }

  function requestCreate(): void {
    const normalized = query.trim();
    if (normalized.length === 0) {
      props.onCreateNew?.(props.property, props.onChange);
      return;
    }

    setShowDuplicateWarning(false);
    if (submittedQuery === normalized && search.isFetched && !search.isFetching) {
      if (search.error === null && results.length > 0) {
        setShowDuplicateWarning(true);
      } else if (search.error === null) {
        props.onCreateNew?.(props.property, props.onChange, normalized);
      } else {
        setPendingCreateQuery(normalized);
        void search.refetch();
      }
      return;
    }

    setPendingCreateQuery(normalized);
    setSubmittedQuery(normalized);
  }

  function createDespiteWarning(): void {
    setShowDuplicateWarning(false);
    props.onCreateNew?.(props.property, props.onChange, query.trim() || undefined);
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
          onChange={event => {
            setQuery(event.target.value);
            setShowDuplicateWarning(false);
            setPendingCreateQuery(null);
          }}
          onKeyDown={event => {
            if (event.key === "Enter") {
              event.preventDefault();
              submitSearch();
            }
          }}
          placeholder="Найдите сущность по имени"
        />
        <button
          className="button subtle compact"
          type="button"
          onClick={submitSearch}
          disabled={search.isFetching}
        >
          {search.isFetching && pendingCreateQuery === null ? "Поиск…" : "Найти"}
        </button>
        {props.onCreateNew && canCreateTarget && (
          <button
            className="button subtle compact"
            type="button"
            onClick={requestCreate}
            disabled={pendingCreateQuery !== null}
          >
            {pendingCreateQuery !== null ? "Проверка…" : "Создать новую сущность"}
          </button>
        )}
      </div>
      {error && <span className="notice error">{error}</span>}
      {showDuplicateWarning && (
        <div className="notice warning duplicate-warning">
          <div>
            <strong>Возможно, такая сущность уже существует</strong>
            <span>Проверьте найденные варианты перед созданием новой записи.</span>
          </div>
          <button
            className="button subtle compact"
            type="button"
            onClick={createDespiteWarning}
          >
            Всё равно создать новую
          </button>
        </div>
      )}
      {!search.isFetching && submittedQuery.length > 0 && results.length === 0 && !error && (
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
                setSubmittedQuery("");
                setQuery(result.displayName);
                setShowDuplicateWarning(false);
                setPendingCreateQuery(null);
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
