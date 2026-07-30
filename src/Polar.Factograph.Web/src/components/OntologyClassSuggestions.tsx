import type { OntologyClassSearchSuggestion } from "../api/models";

interface OntologyClassSuggestionsProps {
  suggestions: OntologyClassSearchSuggestion[];
  loading: boolean;
  error: string | null;
  onSelect: (classId: string) => void;
}

export function OntologyClassSuggestions(props: OntologyClassSuggestionsProps) {
  if (props.loading && props.suggestions.length === 0) {
    return <div className="ontology-class-suggestions loading">Ищем подходящие категории…</div>;
  }
  if (props.error !== null || props.suggestions.length === 0) {
    return null;
  }

  return (
    <section className="ontology-class-suggestions" aria-label="Категории из онтологии">
      <span className="ontology-class-caption">Категории</span>
      <div>
        {props.suggestions.map(suggestion => (
          <button
            key={suggestion.classId}
            type="button"
            onClick={() => props.onSelect(suggestion.classId)}
          >
            <strong>{suggestion.label}</strong>
            <span>
              Показать все сущности категории
              {suggestion.isAbstract ? " и её дочерних типов" : ""}
            </span>
          </button>
        ))}
      </div>
    </section>
  );
}
