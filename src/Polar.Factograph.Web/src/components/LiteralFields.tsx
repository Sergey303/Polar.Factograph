import type { PresentedLiteral } from "../api/models";

interface LiteralFieldsProps {
  fields: PresentedLiteral[];
}

export function LiteralFields({ fields }: LiteralFieldsProps) {
  const visible = fields.filter(field => !field.value.startsWith("iiss://"));
  if (visible.length === 0) return null;

  return (
    <dl className="field-list" aria-label="Основные данные">
      {visible.map((field, index) => (
        <div key={`${field.predicate}-${index}`}>
          <dt>{field.label}</dt>
          <dd>
            {field.displayValue}
            {field.language && <span className="language">{field.language}</span>}
          </dd>
        </div>
      ))}
    </dl>
  );
}
