import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
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
    onCreated: (resourceId: string) => void
  ) => void;
}

export function ResourceReferenceInput(props: ResourceReferenceInputProps) {
  const [query, setQuery] = useState("");
  const [submittedQuery, setSubmittedQuery] = useState("");
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

  function submitSearch(): void {
    setSubmittedQuery(query.trim());
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
          {search.isFetching ? "Поиск…" : "Найти"}
        </button>
        {props.onCreateNew && canCreateTarget && (
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
