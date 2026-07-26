import { DocumentCard } from "./DocumentCard";

interface DocumentSectionProps {
  uris: string[];
  token: string;
}

export function DocumentSection({ uris, token }: DocumentSectionProps) {
  if (uris.length === 0) return null;

  return (
    <section className="portrait-section">
      <h3>Документы</h3>
      <div className="document-grid">
        {uris.map(uri => (
          <DocumentCard key={uri} uri={uri} token={token} />
        ))}
      </div>
    </section>
  );
}
