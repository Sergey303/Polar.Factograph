import type { SemanticResourceLink } from "../api/models";
import { resourceHref } from "../app/routes";

interface SemanticLinkSectionProps {
  title: string;
  links: SemanticResourceLink[];
}

export function SemanticLinkSection({ title, links }: SemanticLinkSectionProps) {
  if (links.length === 0) return null;

  return (
    <section className="semantic-section">
      <h2>{title}</h2>
      <ul className="semantic-link-list">
        {links.map(link => (
          <li key={link.resourceId}>
            <a href={resourceHref(link.resourceId)}>{link.displayName}</a>
            <span className="muted">
              {link.relationLabel}
              {link.typeLabel ? ` · ${link.typeLabel}` : ""}
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}
