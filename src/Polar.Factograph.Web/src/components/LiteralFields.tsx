import type { PresentedLiteral } from "../api/models";

interface LiteralFieldsProps {
  fields: PresentedLiteral[];
}

export function LiteralFields({ fields }: LiteralFieldsProps) {
  const visible = fields.filter(field => !field.value.startsWith("iiss://"));
  if (visible.length === 0) return null;

  return (
    <section className="portrait-section">
      <h3>Сведения</h3>
      <dl className="field-list">
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
    </section>
  );
}
