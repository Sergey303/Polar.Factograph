import type { PreviewSubsystemStatus } from "../api/adminModels";
import {
  formatAdminDate,
  formatAdminNumber,
  workerStateLabel
} from "../app/adminFormat";
import { AdminMetricGrid } from "./AdminMetricGrid";
import { AdminPreviewCassetteTable } from "./AdminPreviewCassetteTable";

interface AdminPreviewCardProps {
  status: PreviewSubsystemStatus | null;
}

export function AdminPreviewCard({ status }: AdminPreviewCardProps) {
  if (status === null) {
    return (
      <section className="admin-card">
        <span className="eyebrow">Превью</span>
        <h2>Обработчик документов</h2>
        <p className="muted">Состояние обработчика пока не загружено.</p>
      </section>
    );
  }

  const items = [
    { label: "Состояние", value: workerStateLabel(status.health.state) },
    { label: "В очереди", value: formatAdminNumber(status.queue.queued) },
    { label: "В работе", value: formatAdminNumber(status.queue.processing) },
    { label: "Неудачных", value: formatAdminNumber(status.queue.failed) },
    { label: "Обработано всего", value: formatAdminNumber(status.worker.totalHandled) },
    { label: "Последний успех", value: formatAdminDate(status.worker.lastSuccessAtUtc) }
  ];

  return (
    <section className="admin-card">
      <div className="admin-card-heading">
        <div><span className="eyebrow">Превью</span><h2>Обработчик документов</h2></div>
        <span className={`admin-state ${status.health.degraded ? "danger" : "ok"}`}>
          {workerStateLabel(status.health.state)}
        </span>
      </div>
      <AdminMetricGrid items={items} />
      {status.worker.lastFailureCode && (
        <div className="notice error">
          Последняя ошибка: <span className="mono">{status.worker.lastFailureCode}</span>
        </div>
      )}
      <AdminPreviewCassetteTable cassettes={status.queue.cassettes} />
    </section>
  );
}
