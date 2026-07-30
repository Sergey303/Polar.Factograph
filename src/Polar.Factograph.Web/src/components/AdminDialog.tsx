import { useEffect } from "react";
import { useAdminStatus } from "../app/useAdminStatus";
import { useIndexRebuild } from "../app/useIndexRebuild";
import { useMaterializationSummary } from "../app/useMaterializationSummary";
import { useOntologyValidation } from "../app/useOntologyValidation";
import { AdminIndexCard } from "./AdminIndexCard";
import { AdminMaterializationCard } from "./AdminMaterializationCard";
import { AdminPreviewCard } from "./AdminPreviewCard";
import { OntologyValidationCard } from "./OntologyValidationCard";

interface AdminDialogProps {
  token: string;
  onClose: () => void;
}

export function AdminDialog({ token, onClose }: AdminDialogProps) {
  const status = useAdminStatus(token, true);
  const materialization = useMaterializationSummary(token);
  const ontology = useOntologyValidation(token);
  const rebuild = useIndexRebuild(token, status.reload);
  const refreshing = status.loading || ontology.loading;

  useEffect(() => {
    function closeOnEscape(event: KeyboardEvent): void {
      if (event.key === "Escape" && !rebuild.busy) onClose();
    }
    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [onClose, rebuild.busy]);

  function reload(): void {
    status.reload();
    ontology.reload();
  }

  return (
    <div className="admin-overlay" role="presentation">
      <section className="admin-dialog" role="dialog" aria-modal="true" aria-label="Администрирование">
        <header className="admin-dialog-header">
          <div>
            <span className="eyebrow">Проект</span>
            <h1>Администрирование</h1>
          </div>
          <div className="button-row">
            <button className="button ghost" disabled={refreshing} onClick={reload}>
              {refreshing ? "Обновление…" : "Обновить"}
            </button>
            <button className="button" disabled={rebuild.busy} onClick={onClose}>Закрыть</button>
          </div>
        </header>

        {status.error && <div className="notice error">{status.error}</div>}
        <div className="admin-card-list">
          <AdminIndexCard
            status={status.index}
            result={rebuild.result}
            busy={rebuild.busy}
            error={rebuild.error}
            onRebuild={() => void rebuild.rebuild()}
          />
          <OntologyValidationCard
            report={ontology.report}
            loading={ontology.loading}
            error={ontology.error}
          />
          <AdminPreviewCard status={status.previews} />
          <AdminMaterializationCard
            summary={materialization.summary}
            loading={materialization.loading}
            error={materialization.error}
            onLoad={() => void materialization.load()}
          />
        </div>
      </section>
    </div>
  );
}
