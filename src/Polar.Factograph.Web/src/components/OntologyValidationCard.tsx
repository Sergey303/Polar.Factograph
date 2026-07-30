import type { OntologyValidationReport } from "../api/adminModels";

interface OntologyValidationCardProps {
  report: OntologyValidationReport | null;
  loading: boolean;
  error: string | null;
}

export function OntologyValidationCard(props: OntologyValidationCardProps) {
  return (
    <section className="admin-card ontology-validation-card">
      <header>
        <div>
          <h3>Онтология</h3>
          <p>Проверка контрактов универсального просмотра и редактирования.</p>
        </div>
        {props.report !== null && (
          <span className={`ontology-validation-state ${props.report.isValid ? "valid" : "invalid"}`}>
            {props.report.isValid ? "Структура допустима" : "Найдены ошибки"}
          </span>
        )}
      </header>

      {props.loading && props.report === null && (
        <div className="admin-card-loading">Проверяем онтологию…</div>
      )}
      {props.error !== null && (
        <div className="notice error">{props.error}</div>
      )}

      {props.report !== null && (
        <>
          <div className="ontology-validation-summary">
            <span><strong>{props.report.termCount}</strong> терминов</span>
            <span className={props.report.errorCount > 0 ? "error-count" : ""}>
              <strong>{props.report.errorCount}</strong> ошибок
            </span>
            <span className={props.report.warningCount > 0 ? "warning-count" : ""}>
              <strong>{props.report.warningCount}</strong> предупреждений
            </span>
          </div>

          {props.report.issues.length === 0 ? (
            <p className="muted ontology-validation-empty">
              Ссылки классов и свойства, необходимые универсальному интерфейсу, согласованы.
            </p>
          ) : (
            <details className="ontology-validation-details" open={props.report.errorCount > 0}>
              <summary>Показать замечания</summary>
              <ol>
                {props.report.issues.map((issue, index) => (
                  <li className={issue.severity} key={`${issue.termId}:${issue.code}:${index}`}>
                    <div>
                      <strong>{issue.severity === "error" ? "Ошибка" : "Предупреждение"}</strong>
                      <code>{issue.code}</code>
                    </div>
                    <span>{issue.message}</span>
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
