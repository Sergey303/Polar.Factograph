import type { PresentedLiteral } from "../api/models";

interface ResourceLiteralSummaryProps {
  fields: PresentedLiteral[];
}

function isDateField(field: PresentedLiteral): boolean {
  return /date/i.test(field.predicate) || /дата/i.test(field.label);
}

function isLongValue(value: string): boolean {
  return value.length > 64 || value.includes("\n");
}

export function ResourceLiteralSummary({ fields }: ResourceLiteralSummaryProps) {
  const seen = new Set<string>();
  const visible = fields.filter(field => {
    const value = field.displayValue.trim();
    if (value.length === 0 || field.value.trim().startsWith("iiss://")) return false;

    const identity = value.toLocaleLowerCase("ru-RU");
    if (seen.has(identity)) return false;
    seen.add(identity);
    return true;
  });

  if (visible.length === 0) return null;

  return (
    <div className="resource-literal-summary" aria-label="Основные сведения">
      {visible.map((field, index) => {
        const value = field.displayValue.trim();
        const className = isLongValue(value)
          ? "resource-summary-value is-long"
          : "resource-summary-value is-short";
        const key = `${field.predicate}:${value}:${index}`;
        return isDateField(field) ? (
          <time className={`${className} is-date`} key={key}>{value}</time>
        ) : (
          <span className={className} key={key}>{value}</span>
        );
      })}
    </div>
  );
}
