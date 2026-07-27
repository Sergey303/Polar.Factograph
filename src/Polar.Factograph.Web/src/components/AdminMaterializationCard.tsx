import type { FogMaterializationStatistics } from "../api/adminModels";
import { formatAdminNumber } from "../app/adminFormat";
import { AdminMetricGrid } from "./AdminMetricGrid";

interface AdminMaterializationCardProps {
  summary: FogMaterializationStatistics | null;
  loading: boolean;
  error: string | null;
  onLoad: () => void;
}

export function AdminMaterializationCard(props: AdminMaterializationCardProps) {
  const items = props.summary === null ? [] : [
    { label: "Fog-файлов", value: formatAdminNumber(props.summary.sourceFiles) },
    { label: "Исходных записей", value: formatAdminNumber(props.summary.sourceRecords) },
    { label: "Определений ресурсов", value: formatAdminNumber(props.summary.resourceDefinitions) },
    { label: "Текущих ресурсов", value: formatAdminNumber(props.summary.currentSourceResources) },
    { label: "Текущих свойств", value: formatAdminNumber(props.summary.currentProperties) },
    { label: "Удалений", value: formatAdminNumber(props.summary.deleteOperations) },
    { label: "Замен", value: formatAdminNumber(props.summary.substituteOperations) },
    { label: "Дубликатов ID", value: formatAdminNumber(props.summary.duplicateResourceIds) }
  ];

  return (
    <section className="admin-card">
      <div className="admin-card-heading">
        <div><span className="eyebrow">Fog/XML</span><h2>Материализация</h2></div>
        <button className="button" disabled={props.loading} onClick={props.onLoad}>
          {props.loading ? "Подсчёт…" : props.summary ? "Пересчитать" : "Рассчитать"}
        </button>
      </div>
      <p className="muted">
        Расчёт повторно читает все Fog-записи и поэтому запускается только вручную.
      </p>
      {props.summary && <AdminMetricGrid items={items} />}
      {props.error && <div className="notice error">{props.error}</div>}
    </section>
  );
}
