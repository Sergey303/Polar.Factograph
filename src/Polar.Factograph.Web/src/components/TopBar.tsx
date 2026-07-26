import { useEffect, useState } from "react";
import type { ProjectOverview } from "../api/models";

interface TopBarProps {
  project: ProjectOverview | null;
  token: string;
  loading: boolean;
  onTokenSave: (value: string) => void;
  onReload: () => void;
}

export function TopBar({
  project,
  token,
  loading,
  onTokenSave,
  onReload
}: TopBarProps) {
  const [draft, setDraft] = useState(token);
  const [expanded, setExpanded] = useState(false);

  useEffect(() => setDraft(token), [token]);

  function save(): void {
    onTokenSave(draft);
    setExpanded(false);
  }

  return (
    <header className="top-bar">
      <div className="brand-block">
        <span className="brand-mark">PF</span>
        <div>
          <strong>Polar.Factograph</strong>
          <div className="muted top-caption">
            {project?.name ?? "Подключение к проекту"}
          </div>
        </div>
      </div>

      <div className="top-actions">
        {project && (
          <span className="user-pill" title={project.userId}>
            {project.userId}
          </span>
        )}
        <button className="button ghost" onClick={onReload} disabled={loading}>
          Обновить
        </button>
        <button className="button" onClick={() => setExpanded(value => !value)}>
          Доступ
        </button>
      </div>

      {expanded && (
        <div className="token-popover">
          <label htmlFor="access-token">JWT для текущей сессии</label>
          <textarea
            id="access-token"
            value={draft}
            onChange={event => setDraft(event.target.value)}
            rows={4}
            placeholder="В режиме разработки поле можно оставить пустым"
          />
          <div className="token-actions">
            <button className="button ghost" onClick={() => setExpanded(false)}>
              Отмена
            </button>
            <button className="button primary" onClick={save}>
              Подключить
            </button>
          </div>
        </div>
      )}
    </header>
  );
}
