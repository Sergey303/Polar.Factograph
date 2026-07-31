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
    { label: "Активное поколение", value: props.status.currentGenerationId ?? "—" },
    {
      label: "Активное поколение доступно",
      value: props.status.currentGenerationAvailable ? "да" : "нет"
    },
    { label: "DIRTY с", value: formatAdminDate(props.status.dirtySinceUtc) },
    { label: "Завершённых поколений", value: props.status.completedGenerationCount },
    { label: "Строящихся поколений", value: props.status.buildingGenerationCount }
  ];
  const resultItems = props.result === null ? [] : [
    { label: "Кассет в конфигурации", value: formatAdminNumber(props.result.enabledCassettes) },
    { label: "Просканировано кассет", value: formatAdminNumber(props.result.scannedCassettes) },
    { label: "FOG-файлов", value: formatAdminNumber(props.result.sourceFiles) },
    { label: "Редакторов в настройках", value: formatAdminNumber(props.result.editors.configuredEditors) },
    { label: "Зарегистрировано редакторов", value: formatAdminNumber(props.result.editors.registeredEditors) },
    { label: "Редакторов без регистрации", value: formatAdminNumber(props.result.editors.unregisteredEditors) },
    { label: "Проверено FOG редакторов", value: formatAdminNumber(props.result.editors.validEditorFogs) },
    { label: "Неназначенных записываемых FOG", value: formatAdminNumber(props.result.editors.unassignedWritableFogs) },
    { label: "Ресурсов", value: formatAdminNumber(props.result.statistics.resources) },
    { label: "Троек", value: formatAdminNumber(props.result.statistics.triples) },
    { label: "Строк поиска по имени", value: formatAdminNumber(props.result.statistics.nameSearchRows) },
    { label: "Строк полнотекстового поиска", value: formatAdminNumber(props.result.statistics.wordSearchRows) },
    { label: "Поколение", value: props.result.generationId }
  ];

  return (
    <section className="admin-card">
      <div className="admin-card-heading">
        <div><span className="eyebrow">Polar.DB</span><h2>Индекс проекта</h2></div>
        <button className="button danger" disabled={props.busy} onClick={props.onRebuild}>
          {props.busy ? "Обновление…" : "Обновить индекс"}
        </button>
      </div>
      <p className="muted">
        Перечитывает конфигурацию кассет, проверяет FOG редакторов и создаёт новое полное
        поколение опорной последовательности и поисковых индексов Polar.DB.
      </p>
      {props.status && <AdminMetricGrid items={statusItems} />}
      {!props.status && <p className="muted">Состояние индекса пока не загружено.</p>}
      {props.error && <div className="notice error">{props.error}</div>}
      {props.result && (
        <div className="admin-result">
          <strong>Индекс проекта обновлён</strong>
          <AdminMetricGrid items={resultItems} />
        </div>
      )}
    </section>
  );
}
