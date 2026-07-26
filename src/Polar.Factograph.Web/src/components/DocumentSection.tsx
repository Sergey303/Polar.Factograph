import type { ProjectOverview } from "../api/models";
import { DocumentCard } from "./DocumentCard";

interface DocumentSectionProps {
  uris: string[];
  token: string;
  project: ProjectOverview | null;
}

export function DocumentSection({ uris, token, project }: DocumentSectionProps) {
  if (uris.length === 0) return null;

  return (
    <section className="portrait-section">
      <h3>Документы</h3>
      <div className="document-grid">
        {uris.map(uri => (
          <DocumentCard key={uri} uri={uri} token={token} project={project} />
        ))}
      </div>
    </section>
  );
}
