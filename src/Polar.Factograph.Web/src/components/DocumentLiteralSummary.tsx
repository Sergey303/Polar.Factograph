import type { PresentedLiteral } from "../api/models";

interface DocumentLiteralSummaryProps {
  fields: PresentedLiteral[];
}

function isDatePredicate(predicate: string): boolean {
  return /(^|[/#])(date|start-date|begin-date|end-date)$/i.test(predicate);
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
          className={isDatePredicate(field.predicate)
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
