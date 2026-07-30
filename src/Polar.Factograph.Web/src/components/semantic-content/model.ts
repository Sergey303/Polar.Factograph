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
  hasDocument: boolean;
}

export interface SemanticContentItem {
  key: string;
  resourceId: string;
  title: string;
  members: SemanticContentMember[] | null;
  values: string[];
  sectionKey: string;
  sectionTitle: string;
  documentUri: string | null;
  hasDocument: boolean;
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

function isPublicLabel(value: string | null | undefined): value is string {
  if (!value || value.trim().length === 0) return false;
  const normalized = value.trim().toLocaleLowerCase("ru-RU");
  return !normalized.startsWith("http://") &&
    !normalized.startsWith("https://") &&
    !normalized.startsWith("urn:");
}

function firstPublicLabel(...values: Array<string | null | undefined>): string | null {
  return values.find(isPublicLabel)?.trim() ?? null;
}

function entryLabel(entry: SemanticRelationEntry, documentBacked: boolean): string | null {
  if (documentBacked) return null;
  return firstPublicLabel(
    entry.relationTypeLabel,
    entry.groupLabel,
    entry.title,
    entry.members.find(member => isPublicLabel(member.roleLabel))?.roleLabel);
}

function publicValues(values: string[], label: string | null): string[] {
  const result = label === null ? [...values] : [label, ...values];
  return result.filter((value, index) =>
    value.trim().length > 0 && result.indexOf(value) === index);
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
      members: null,
      values: photo.contextLabel ? [photo.contextLabel] : [],
      sectionKey: key,
      sectionTitle: title,
      documentUri: photo.documentUri,
      hasDocument: hasDocument(photo),
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
    items: links.map(link => {
      const documentBacked = hasDocument(link);
      const label = documentBacked
        ? null
        : firstPublicLabel(link.groupLabel, link.relationLabel, link.typeLabel);
      return {
        key: `${key}:${link.relationResourceId ?? link.resourceId}:${link.resourceId}`,
        resourceId: link.resourceId,
        title: link.displayName,
        members: null,
        values: publicValues([], label),
        sectionKey: key,
        sectionTitle: title,
        documentUri: link.documentUri ?? null,
        hasDocument: documentBacked,
        displayDate: link.displayDate ?? null,
        sortDate: link.sortDate ?? null
      };
    })
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
      const documentMember = entry.members.find(member =>
        hasDocument(member) && member.documentUri === entry.documentUri) ??
        entry.members.find(hasDocument);
      const previewMember = documentMember ?? entry.members[0];
      const relationOwnsDocument = documentMember === undefined && entry.documentUri !== null;
      const documentBacked = documentMember !== undefined || entry.documentUri !== null;
      const values = (entry.values ?? []).map(value => value.value).filter(Boolean);
      return {
        key: `${key}:${entry.key}`,
        resourceId: relationOwnsDocument
          ? entry.relationResourceId ?? entry.key
          : previewMember?.resourceId ?? entry.relationResourceId ?? entry.key,
        title: entry.title,
        members: entry.members.map(member => ({
          resourceId: member.resourceId,
          displayName: member.displayName,
          roleLabel: member.roleLabel,
          documentUri: member.documentUri,
          hasDocument: hasDocument(member)
        })),
        values: publicValues(values, entryLabel(entry, documentBacked)),
        sectionKey: key,
        sectionTitle: title,
        documentUri: documentMember?.documentUri ?? entry.documentUri,
        hasDocument: documentBacked,
        displayDate: entry.displayDate,
        sortDate: entry.sortDate
      };
    })
  };
}
