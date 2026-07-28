import type { ProjectOverview } from "../api/models";
import { DocumentCard } from "./DocumentCard";

interface DocumentSectionProps {
  uris: string[];
  token: string;
  project: ProjectOverview | null;
  previewOnly?: boolean;
  minimumPreviewImageWidth?: number;
}

export function DocumentSection({
  uris,
  token,
  project,
  previewOnly = false,
  minimumPreviewImageWidth = 0
}: DocumentSectionProps) {
  if (uris.length === 0) return null;

  return (
    <section className="portrait-section">
      <h3>Документы</h3>
      <div className="document-grid">
        {uris.map(uri => (
          <DocumentCard
            key={uri}
            uri={uri}
            token={token}
            project={project}
            previewOnly={previewOnly}
            minimumPreviewImageWidth={minimumPreviewImageWidth}
          />
        ))}
      </div>
    </section>
  );
}
