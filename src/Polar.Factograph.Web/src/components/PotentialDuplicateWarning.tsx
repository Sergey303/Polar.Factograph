import type { PotentialDuplicateResource } from "../api/models";
import { resourceHref } from "../app/routes";

interface PotentialDuplicateWarningProps {
  candidates: PotentialDuplicateResource[];
  error: string | null;
  onUseExisting?: (resourceId: string) => void;
  onContinue: () => void;
}

export function PotentialDuplicateWarning(
  props: PotentialDuplicateWarningProps
) {
  if (props.error !== null) {
    return (
      <div className="notice warning duplicate-save-warning">
        <div>
          <strong>Не удалось проверить возможные совпадения</strong>
          <span>{props.error}</span>
        </div>
        <button
          className="button subtle compact"
          type="button"
          onClick={props.onContinue}
        >
          Сохранить без проверки
        </button>
      </div>
    );
  }

  if (props.candidates.length === 0) return null;

  return (
    <section className="duplicate-save-warning" aria-labelledby="duplicate-save-title">
      <header>
        <div>
          <strong id="duplicate-save-title">Возможно, такая сущность уже существует</strong>
          <span>Совпали значения введённых строковых полей. Проверьте записи перед созданием новой.</span>
        </div>
        <button
          className="button subtle compact"
          type="button"
          onClick={props.onContinue}
        >
          Всё равно сохранить новую
        </button>
      </header>
      <div className="duplicate-candidate-list">
        {props.candidates.map(candidate => (
          <article className="duplicate-candidate" key={candidate.resourceId}>
            <div>
              <a
                href={resourceHref(candidate.resourceId)}
                target="_blank"
                rel="noopener noreferrer"
              >
                {candidate.displayName}
              </a>
              <span>
                {candidate.typeLabel ?? candidate.type ?? "Сущность"}
                {candidate.alternativeWriting ? " · совпадение по другому написанию" : ""}
              </span>
              <small>{candidate.matchedValue}</small>
            </div>
            {props.onUseExisting && (
              <button
                className="button primary compact"
                type="button"
                onClick={() => props.onUseExisting?.(candidate.resourceId)}
              >
                Использовать
              </button>
            )}
          </article>
        ))}
      </div>
    </section>
  );
}
