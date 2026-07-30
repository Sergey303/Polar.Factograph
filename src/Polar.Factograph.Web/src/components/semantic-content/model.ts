import type {
  SemanticPhotoCard,
  SemanticRelationEntry,
  SemanticResourceLink
} from "../../api/models";

export type BlockLayout = "list" | "table" | "small" | "medium" | "large";
export type BlockKind = "media" | "text";

export interface SemanticContentMember {
  resourceId: string;
  displayName: string;
  roleLabel: string | null;
  documentUri: string | null;
  hideDisplayName: boolean;
}

export interface SemanticContentItem {
  key: string;
  resourceId: string;
  title: string;
  detail: string | null;
  relationLabel: string | null;
  typeLabel: string | null;
  members: SemanticContentMember[] | null;
  sectionKey: string;
  sectionTitle: string;
  documentUri: string | null;
  hideDisplayName: boolean;
  displayDate: string | null;
  sortDate: string | null;
}

export interface SemanticContentBlockDefinition {
  key: string;
  title: string;
  kind: BlockKind;
  items: SemanticContentItem[];
}

function hasDocument(value: {
  documentUri?: string | null;
  hasDocument?: boolean;
}): boolean {
  return value.hasDocument === true || value.documentUri != null;
}

export function photoBlock(
  key: string,
  title: string,
  photos: SemanticPhotoCard[]
): SemanticContentBlockDefinition {
  return {
    key,
    title,
    kind: "media",
    items: photos.map(photo => ({
      key: `${key}:${photo.resourceId}:${photo.contextResourceId ?? ""}`,
      resourceId: photo.resourceId,
      title: photo.displayName,
      detail: photo.contextLabel,
      relationLabel: null,
      typeLabel: null,
      members: null,
      sectionKey: key,
      sectionTitle: title,
      documentUri: photo.documentUri,
      hideDisplayName: hasDocument(photo),
      displayDate: photo.displayDate ?? null,
      sortDate: photo.sortDate ?? null
    }))
  };
}

export function linkBlock(
  key: string,
  title: string,
  links: SemanticResourceLink[]
): SemanticContentBlockDefinition {
  return {
    key,
    title,
    kind: links.some(hasDocument) ? "media" : "text",
    items: links.map(link => ({
      key: `${key}:${link.relationResourceId ?? link.resourceId}:${link.resourceId}:${link.relationLabel}`,
      resourceId: link.resourceId,
      title: link.displayName,
      detail: null,
      relationLabel: link.relationLabel,
      typeLabel: link.typeLabel,
      members: null,
      sectionKey: key,
      sectionTitle: title,
      documentUri: link.documentUri ?? null,
      hideDisplayName: hasDocument(link),
      displayDate: link.displayDate ?? null,
      sortDate: link.sortDate ?? null
    }))
  };
}

export function relationEntryBlock(
  key: string,
  title: string,
  entries: SemanticRelationEntry[]
): SemanticContentBlockDefinition {
  return {
    key,
    title,
    kind: entries.some(entry =>
      entry.documentUri !== null || entry.members.some(hasDocument))
      ? "media"
      : "text",
    items: entries.map(entry => {
      const previewMember = entry.members.find(member =>
        member.documentUri !== null && member.documentUri === entry.documentUri) ??
        entry.members.find(member => member.documentUri !== null) ??
        entry.members[0];
      const detail = entry.relationTypeLabel && entry.relationTypeLabel !== entry.title
        ? entry.relationTypeLabel
        : null;
      return {
        key: `${key}:${entry.key}`,
        resourceId: previewMember?.resourceId ?? entry.relationResourceId ?? entry.key,
        title: entry.title,
        detail,
        relationLabel: entry.title,
        typeLabel: detail,
        members: entry.members.map(member => ({
          resourceId: member.resourceId,
          displayName: member.displayName,
          roleLabel: member.roleLabel,
          documentUri: member.documentUri,
          hideDisplayName: hasDocument(member)
        })),
        sectionKey: key,
        sectionTitle: title,
        documentUri: previewMember?.documentUri ?? entry.documentUri,
        hideDisplayName: previewMember === undefined ? false : hasDocument(previewMember),
        displayDate: entry.displayDate,
        sortDate: entry.sortDate
      };
    })
  };
}
