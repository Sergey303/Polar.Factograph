import type { SemanticResourcePage } from "../api/models";
import {
  linkBlock,
  photoBlock,
  SemanticContentBlocks
} from "./SemanticContentBlocks";

interface SemanticResourceSectionsProps {
  page: SemanticResourcePage;
}

export function SemanticResourceSections({ page }: SemanticResourceSectionsProps) {
  const blocks = [
    photoBlock("photos", "Фотографии", page.photos),
    linkBlock("participants", "Участники", page.participants),
    linkBlock("organizations", "Организации", page.organizations),
    linkBlock("collections", "Коллекции", page.collections),
    linkBlock("related", "Другие связи", page.relatedResources)
  ];

  return <SemanticContentBlocks blocks={blocks} />;
}
