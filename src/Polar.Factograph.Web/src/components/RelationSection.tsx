import type {
  PresentedDirectLink,
  PresentedInverseLink
} from "../api/models";

interface RelationSectionProps {
  direct: PresentedDirectLink[];
  inverse: PresentedInverseLink[];
  onSelect: (resourceId: string) => void;
}

export function RelationSection({ direct, inverse, onSelect }: RelationSectionProps) {
  if (direct.length === 0 && inverse.length === 0) return null;

  return (
    <section className="portrait-section relations">
      <h3>Связи</h3>
      {direct.map((link, index) => (
        <button
          className="relation-row"
          key={`direct-${link.predicate}-${link.targetResourceId}-${index}`}
          onClick={() => onSelect(link.targetResourceId)}
        >
          <span>{link.label}</span>
          <strong>{link.targetResourceId}</strong>
          <span aria-hidden="true">→</span>
        </button>
      ))}
      {inverse.map((link, index) => (
        <button
          className="relation-row inverse"
          key={`inverse-${link.predicate}-${link.sourceResourceId}-${index}`}
          onClick={() => onSelect(link.sourceResourceId)}
        >
          <span>{link.label}</span>
          <strong>{link.sourceResourceId}</strong>
          <span aria-hidden="true">←</span>
        </button>
      ))}
    </section>
  );
}
