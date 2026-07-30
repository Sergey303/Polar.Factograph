import type { OntologyValidationReport } from "../api/adminModels";

interface OntologyValidationCardProps {
  report: OntologyValidationReport | null;
  loading: boolean;
  error: string | null;
}

function validationState(report: OntologyValidationReport): {
  className: string;
  label: string;
} {
  if (report.errorCount > 0) {
    return { className: "danger", label: "Нужны исправления" };
  }
  if (report.warningCount > 0) {
    return { className: "warning", label: "Есть предупреждения" };
  }
  return { className: "ok", label: "Проверка пройдена" };
}

export function OntologyValidationCard(props: OntologyValidationCardProps) {
  const state = props.report === null ? null : validationState(props.report);

  return (
    <section className="admin-card ontology-validation-card">
      <div className="admin-card-heading ontology-validation-heading">
        <div>
          <span className="eyebrow">Схема данных</span>
          <h2>Онтология</h2>
          <p>Контракты универсального просмотра, связей и редактирования.</p>
        </div>
        {state !== null && (
          <span className={`admin-state ${state.className}`}>{state.label}</span>
        )}
      </div>

      {props.loading && props.report === null && (
        <p className="muted">Проверяем онтологию…</p>
      )}
      {props.error !== null && (
        <div className="notice error">{props.error}</div>
      )}

      {props.report !== null && (
        <>
          <dl className="ontology-validation-summary" aria-label="Результат проверки онтологии">
            <div>
              <dt>Терминов</dt>
              <dd>{props.report.termCount}</dd>
            </div>
            <div className={props.report.errorCount > 0 ? "danger" : ""}>
              <dt>Ошибок</dt>
              <dd>{props.report.errorCount}</dd>
            </div>
            <div className={props.report.warningCount > 0 ? "warning" : ""}>
              <dt>Предупреждений</dt>
              <dd>{props.report.warningCount}</dd>
            </div>
          </dl>

          {props.report.issues.length === 0 ? (
            <p className="muted ontology-validation-empty">
              Ссылки классов и свойства, необходимые универсальному интерфейсу, согласованы.
            </p>
          ) : (
            <details
              className="ontology-validation-details"
              open={props.report.errorCount > 0}
            >
              <summary>Замечания: {props.report.issues.length}</summary>
              <ol>
                {props.report.issues.map((issue, index) => (
                  <li className={issue.severity} key={`${issue.termId}:${issue.code}:${index}`}>
                    <div className="ontology-validation-issue-heading">
                      <strong>{issue.severity === "error" ? "Ошибка" : "Предупреждение"}</strong>
                      <code>{issue.code}</code>
                    </div>
                    <p>{issue.message}</p>
                    <code className="ontology-term-id">{issue.termId}</code>
                  </li>
                ))}
              </ol>
            </details>
          )}
        </>
      )}
    </section>
  );
}
