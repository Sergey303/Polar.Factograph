import type { ProjectOverview } from "../api/models";
import type { DocumentPreviewPolicy } from "../app/useDocumentAsset";
import { DocumentCard } from "./DocumentCard";

interface DocumentSectionProps {
  uris: string[];
  token: string;
  project: ProjectOverview | null;
  title?: string;
  previewPolicy?: DocumentPreviewPolicy;
  imageDocument?: boolean;
  allowReplace?: boolean;
}

export function DocumentSection({
  uris,
  token,
  project,
  title = "Документы",
  previewPolicy = "smallest",
  imageDocument = false,
  allowReplace = false
}: DocumentSectionProps) {
  if (uris.length === 0) return null;

  return (
    <section className="portrait-section">
      <h3>{title}</h3>
      <div className="document-grid">
        {uris.map(uri => (
          <DocumentCard
            key={uri}
            uri={uri}
            token={token}
            project={project}
            previewPolicy={previewPolicy}
            imageDocument={imageDocument}
            allowReplace={allowReplace}
          />
        ))}
      </div>
    </section>
  );
}
