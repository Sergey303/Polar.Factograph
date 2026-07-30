import type { ResourcePortrait } from "../api/models";

interface TechnicalResourceDetailsProps {
  portrait: ResourcePortrait;
}

function formatTimestamp(value: string): string {
  const parsed = new Date(value);
  return Number.isNaN(parsed.valueOf())
    ? value
    : parsed.toLocaleString("ru-RU");
}

export function TechnicalResourceDetails({ portrait }: TechnicalResourceDetailsProps) {
  const provenance = portrait.provenance;

  return (
    <details className="technical-resource-details">
      <summary>Технические сведения</summary>
      <dl>
        <div>
          <dt>Идентификатор сущности</dt>
          <dd><code>{portrait.resourceId}</code></dd>
        </div>
        {portrait.type !== null && (
          <div>
            <dt>Тип</dt>
            <dd><code>{portrait.type}</code></dd>
          </div>
        )}
        {provenance !== null && (
          <div>
            <dt>Исходная кассета</dt>
            <dd><code>{provenance.sourceCassetteId}</code></dd>
          </div>
        )}
        {provenance?.sourceRecordId !== null && provenance?.sourceRecordId !== undefined && (
          <div>
            <dt>Исходная запись</dt>
            <dd><code>{provenance.sourceRecordId}</code></dd>
          </div>
        )}
        {provenance?.sourceFogPath !== null && provenance?.sourceFogPath !== undefined && (
          <div>
            <dt>Fog-источник</dt>
            <dd><code>{provenance.sourceFogPath}</code></dd>
          </div>
        )}
        {provenance?.modifiedAt !== null && provenance?.modifiedAt !== undefined && (
          <div>
            <dt>Время исходной ревизии</dt>
            <dd><time dateTime={provenance.modifiedAt}>{formatTimestamp(provenance.modifiedAt)}</time></dd>
          </div>
        )}
      </dl>
    </details>
  );
}
