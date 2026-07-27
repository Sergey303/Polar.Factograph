import type { SemanticResourcePage } from "../api/models";
import { SemanticLinkSection } from "./SemanticLinkSection";
import { SemanticPhotoGallery } from "./SemanticPhotoGallery";

interface SemanticResourceSectionsProps {
  page: SemanticResourcePage;
}

export function SemanticResourceSections({ page }: SemanticResourceSectionsProps) {
  return (
    <div className="semantic-resource-sections">
      <SemanticPhotoGallery photos={page.photos} />
      <SemanticLinkSection title="Участники" links={page.participants} />
      <SemanticLinkSection title="Организации" links={page.organizations} />
      <SemanticLinkSection title="Коллекции" links={page.collections} />
      <SemanticLinkSection title="Связанные сущности" links={page.relatedResources} />
    </div>
  );
}
