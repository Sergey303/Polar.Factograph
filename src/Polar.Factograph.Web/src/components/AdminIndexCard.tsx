import type {
  ProjectIndexRebuildResult,
  ProjectIndexRuntimeStatus,
  ProjectIndexVerificationReport
} from "../api/adminModels";
import {
  formatAdminDate,
  formatAdminNumber,
  indexStateLabel
} from "../app/adminFormat";
import { AdminMetricGrid } from "./AdminMetricGrid";

interface AdminIndexCardProps {
  status: ProjectIndexRuntimeStatus | null;
  rebuildResult: ProjectIndexRebuildResult | null;
  rebuildBusy: boolean;
  rebuildError: string | null;
  verificationResult: ProjectIndexVerificationReport | null;
  verificationBusy: boolean;
  verificationError: string | null;
  onRebuild: () => void;
  onVerify: () => void;
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
  const rebuildItems = props.rebuildResult === null ? [] : [
    { label: "Кассет в конфигурации", value: formatAdminNumber(props.rebuildResult.enabledCassettes) },
    { label: "Просканировано кассет", value: formatAdminNumber(props.rebuildResult.scannedCassettes) },
    { label: "FOG-файлов", value: formatAdminNumber(props.rebuildResult.sourceFiles) },
    { label: "Редакторов в настройках", value: formatAdminNumber(props.rebuildResult.editors.configuredEditors) },
    { label: "Зарегистрировано редакторов", value: formatAdminNumber(props.rebuildResult.editors.registeredEditors) },
    { label: "Редакторов без регистрации", value: formatAdminNumber(props.rebuildResult.editors.unregisteredEditors) },
    { label: "Проверено FOG редакторов", value: formatAdminNumber(props.rebuildResult.editors.validEditorFogs) },
    { label: "Неназначенных записываемых FOG", value: formatAdminNumber(props.rebuildResult.editors.unassignedWritableFogs) },
    { label: "Ресурсов", value: formatAdminNumber(props.rebuildResult.statistics.resources) },
    { label: "Троек", value: formatAdminNumber(props.rebuildResult.statistics.triples) },
    { label: "Строк поиска по имени", value: formatAdminNumber(props.rebuildResult.statistics.nameSearchRows) },
    { label: "Строк полнотекстового поиска", value: formatAdminNumber(props.rebuildResult.statistics.wordSearchRows) },
    { label: "Поколение", value: props.rebuildResult.generationId }
  ];
  const verificationItems = props.verificationResult === null ? [] : [
    { label: "Поколение", value: props.verificationResult.generationId },
    { label: "FOG-файлов", value: formatAdminNumber(props.verificationResult.sourceFiles) },
    { label: "Ожидалось ресурсов", value: formatAdminNumber(props.verificationResult.expectedResources) },
    { label: "В Polar.DB ресурсов", value: formatAdminNumber(props.verificationResult.storedResources) },
    { label: "Не хватает ресурсов", value: formatAdminNumber(props.verificationResult.missingResources) },
    { label: "Лишних ресурсов", value: formatAdminNumber(props.verificationResult.extraResources) },
    { label: "Ожидалось троек", value: formatAdminNumber(props.verificationResult.expectedTriples) },
    { label: "В Polar.DB троек", value: formatAdminNumber(props.verificationResult.storedTriples) },
    { label: "Не хватает троек", value: formatAdminNumber(props.verificationResult.missingTriples) },
    { label: "Лишних троек", value: formatAdminNumber(props.verificationResult.extraTriples) },
    { label: "Завершено", value: formatAdminDate(props.verificationResult.completedAtUtc) }
  ];
  const anyBusy = props.rebuildBusy || props.verificationBusy;

  return (
    <section className="admin-card">
      <div className="admin-card-heading">
        <div><span className="eyebrow">Polar.DB</span><h2>Индекс проекта</h2></div>
        <div className="button-row">
          <button className="button" disabled={anyBusy} onClick={props.onVerify}>
            {props.verificationBusy ? "Проверка…" : "Проверить индекс"}
          </button>
          <button className="button danger" disabled={anyBusy} onClick={props.onRebuild}>
            {props.rebuildBusy ? "Обновление…" : "Обновить индекс"}
          </button>
        </div>
      </div>
      <p className="muted">
        Обновление перечитывает конфигурацию кассет и создаёт новое поколение Polar.DB.
        Проверка сравнивает с FOG таблицы ресурсов и троек и сохраняет JSON-отчёт.
      </p>
      {props.status && <AdminMetricGrid items={statusItems} />}
      {!props.status && <p className="muted">Состояние индекса пока не загружено.</p>}
      {props.rebuildError && <div className="notice error">{props.rebuildError}</div>}
      {props.verificationError && <div className="notice error">{props.verificationError}</div>}
      {props.rebuildResult && (
        <div className="admin-result">
          <strong>Индекс проекта обновлён</strong>
          <AdminMetricGrid items={rebuildItems} />
        </div>
      )}
      {props.verificationResult && (
        <div className="admin-result">
          <div className={`notice ${props.verificationResult.isMatch ? "success" : "error"}`}>
            <strong>
              {props.verificationResult.isMatch
                ? "Ресурсы и тройки совпадают с FOG"
                : "В ресурсах или тройках обнаружены расхождения"}
            </strong>
          </div>
          <AdminMetricGrid items={verificationItems} />
          <p className="muted admin-report-path">
            JSON-отчёт: <code>{props.verificationResult.reportPath}</code>
          </p>
          {props.verificationResult.differenceSamplesTruncated && (
            <p className="muted">
              В JSON сохранены первые {props.verificationResult.differenceSampleLimit} примеров
              каждого вида расхождений; полные количества указаны выше.
            </p>
          )}
        </div>
      )}
    </section>
  );
}
