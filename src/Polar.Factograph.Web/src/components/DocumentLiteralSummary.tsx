import type { PresentedLiteral } from "../api/models";

interface DocumentLiteralSummaryProps {
  fields: PresentedLiteral[];
}

function isDateField(field: PresentedLiteral): boolean {
  return /date/i.test(field.predicate) || /дата/i.test(field.label);
}

export function DocumentLiteralSummary({ fields }: DocumentLiteralSummaryProps) {
  const seen = new Set<string>();
  const visible = fields.filter(field => {
    const value = field.displayValue.trim();
    if (value.length === 0 || field.value.startsWith("iiss://")) return false;

    const identity = `${field.predicate}\n${value}`;
    if (seen.has(identity)) return false;
    seen.add(identity);
    return true;
  });

  if (visible.length === 0) return null;

  return (
    <section className="document-literal-summary" aria-label="Сведения о фотографии">
      {visible.map((field, index) => (
        <p
          className={isDateField(field)
            ? "document-summary-date"
            : "document-summary-value"}
          key={`${field.predicate}:${index}`}
        >
          {field.displayValue}
        </p>
      ))}
    </section>
  );
}
