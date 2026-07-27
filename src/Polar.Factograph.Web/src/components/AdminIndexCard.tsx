import type {
  ProjectIndexRebuildResult,
  ProjectIndexRuntimeStatus
} from "../api/adminModels";
import {
  formatAdminDate,
  formatAdminNumber,
  indexStateLabel
} from "../app/adminFormat";
import { AdminMetricGrid } from "./AdminMetricGrid";

interface AdminIndexCardProps {
  status: ProjectIndexRuntimeStatus | null;
  result: ProjectIndexRebuildResult | null;
  busy: boolean;
  error: string | null;
  onRebuild: () => void;
}

export function AdminIndexCard(props: AdminIndexCardProps) {
  const statusItems = props.status === null ? [] : [
    { label: "Состояние", value: indexStateLabel(props.status.state) },
    { label: "DIRTY с", value: formatAdminDate(props.status.dirtySinceUtc) },
    { label: "Завершённых поколений", value: props.status.completedGenerationCount },
    { label: "Строящихся поколений", value: props.status.buildingGenerationCount }
  ];
  const resultItems = props.result === null ? [] : [
    { label: "Файлов-источников", value: formatAdminNumber(props.result.sourceFiles) },
    { label: "Ресурсов", value: formatAdminNumber(props.result.statistics.resources) },
    { label: "Троек", value: formatAdminNumber(props.result.statistics.triples) },
    { label: "Поколение", value: props.result.generationId }
  ];

  return (
    <section className="admin-card">
      <div className="admin-card-heading">
        <div><span className="eyebrow">Polar.DB</span><h2>Индекс проекта</h2></div>
        <button className="button danger" disabled={props.busy} onClick={props.onRebuild}>
          {props.busy ? "Перестроение…" : "Перестроить"}
        </button>
      </div>
      {props.status && <AdminMetricGrid items={statusItems} />}
      {!props.status && <p className="muted">Состояние индекса пока не загружено.</p>}
      {props.error && <div className="notice error">{props.error}</div>}
      {props.result && (
        <div className="admin-result">
          <strong>Перестроение завершено</strong>
          <AdminMetricGrid items={resultItems} />
        </div>
      )}
    </section>
  );
}
