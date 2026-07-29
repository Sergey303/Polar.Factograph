import { useState } from "react";
import { documentContentUrl } from "../api/factographApi";
import type { SemanticPhotoCard } from "../api/models";
import { followAppLink, resourceHref } from "../app/routes";

interface SemanticPhotoGalleryProps {
  photos: SemanticPhotoCard[];
}

function SemanticPhoto({ photo }: { photo: SemanticPhotoCard }) {
  const [failed, setFailed] = useState(false);

  return (
    <article className="semantic-photo-card">
      <a
        className="semantic-photo-image"
        href={resourceHref(photo.resourceId)}
        onClick={followAppLink}
      >
        {photo.documentUri !== null && !failed ? (
          <img
            src={documentContentUrl(photo.documentUri, "small")}
            alt={photo.displayName}
            loading="lazy"
            onError={() => setFailed(true)}
          />
        ) : (
          <span>Фотография</span>
        )}
      </a>
      <div className="semantic-photo-caption">
        <a href={resourceHref(photo.resourceId)} onClick={followAppLink}>
          {photo.displayName}
        </a>
        {photo.contextResourceId !== null && photo.contextLabel !== null && (
          <a
            className="muted"
            href={resourceHref(photo.contextResourceId)}
            onClick={followAppLink}
          >
            {photo.contextLabel}
          </a>
        )}
      </div>
    </article>
  );
}

export function SemanticPhotoGallery({ photos }: SemanticPhotoGalleryProps) {
  if (photos.length === 0) return null;

  return (
    <section className="semantic-section">
      <h2>Фотографии</h2>
      <div className="semantic-photo-grid">
        {photos.map(photo => <SemanticPhoto key={photo.resourceId} photo={photo} />)}
      </div>
    </section>
  );
}
