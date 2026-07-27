import type { CassettePreviewQueueStatus } from "../api/adminModels";
import { formatAdminDate } from "../app/adminFormat";

interface AdminPreviewCassetteTableProps {
  cassettes: CassettePreviewQueueStatus[];
}

export function AdminPreviewCassetteTable({ cassettes }: AdminPreviewCassetteTableProps) {
  if (cassettes.length === 0) {
    return <p className="muted">Включённые кассеты не найдены.</p>;
  }

  return (
    <div className="admin-table-wrap">
      <table className="admin-table">
        <thead>
          <tr>
            <th>Кассета</th>
            <th>Очередь</th>
            <th>В работе</th>
            <th>Ошибки</th>
            <th>Старейшая заявка</th>
          </tr>
        </thead>
        <tbody>
          {cassettes.map(cassette => (
            <tr key={cassette.cassetteId}>
              <td>{cassette.cassetteName}</td>
              <td>{cassette.queued}</td>
              <td>{cassette.processing}</td>
              <td>{cassette.failed}</td>
              <td>{formatAdminDate(cassette.oldestQueuedAtUtc)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
