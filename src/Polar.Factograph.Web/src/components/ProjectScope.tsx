import type { ProjectOverview } from "../api/models";

interface ProjectScopeProps {
  project: ProjectOverview | null;
  loading: boolean;
  error: string | null;
}

export function ProjectScope({ project, loading, error }: ProjectScopeProps) {
  return (
    <section className="navigation-section">
      <div className="panel-heading">
        <span className="eyebrow">Область данных</span>
        <h2>Кассеты</h2>
      </div>

      {loading && <p className="muted">Загрузка проекта…</p>}
      {error && <div className="notice error">{error}</div>}

      {project && (
        <>
          <div className="project-summary">
            <strong>{project.name}</strong>
            <span className="muted">{project.projectId}</span>
          </div>
          <ul className="cassette-list">
            {project.cassettes.map(cassette => {
              const isDefault = cassette.id === project.defaultWriteCassetteId;
              return (
                <li key={cassette.id}>
                  <div>
                    <strong>{cassette.name}</strong>
                    <span className="muted mono">{cassette.id}</span>
                  </div>
                  <div className="badge-row">
                    {isDefault && <span className="badge accent">запись</span>}
                    {!cassette.allowWrite && <span className="badge">только чтение</span>}
                  </div>
                </li>
              );
            })}
          </ul>
          {project.defaultWriteCassetteId === null && (
            <div className="notice">Доступна только работа в режиме чтения.</div>
          )}
        </>
      )}
    </section>
  );
}
