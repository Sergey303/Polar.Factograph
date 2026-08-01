import type {
  SemanticRelationEntry,
  SemanticResourceLink,
  SemanticResourcePage
} from "../api/models";
import {
  linkBlock,
  relationEntryBlock,
  SemanticContentBlocks,
  type SemanticContentBlockDefinition
} from "./SemanticContentBlocks";

interface SemanticResourceSectionsProps {
  page: SemanticResourcePage;
  textOnly?: boolean;
}

function linkIdentity(link: SemanticResourceLink): string {
  return [
    link.relationResourceId ?? "",
    link.resourceId,
    link.groupKey ?? link.relationLabel,
    link.relationLabel
  ].join("\n");
}

function legacyLinks(page: SemanticResourcePage): SemanticResourceLink[] {
  return [
    ...page.participants,
    ...page.organizations,
    ...page.collections,
    ...page.relatedResources
  ];
}

function availableLinks(page: SemanticResourcePage): SemanticResourceLink[] {
  return page.links ?? legacyLinks(page);
}

function hasDocument(value: {
  documentUri?: string | null;
  hasDocument?: boolean;
}): boolean {
  return value.hasDocument === true || value.documentUri != null;
}

function entryHasDocument(entry: SemanticRelationEntry): boolean {
  return entry.documentUri !== null || entry.members.some(hasDocument);
}

function entryRepresentsLink(
  entry: SemanticRelationEntry,
  link: SemanticResourceLink
): boolean {
  if (link.relationResourceId !== null && link.relationResourceId !== undefined) {
    return entry.relationResourceId === link.relationResourceId;
  }

  return entry.relationResourceId === null &&
    entry.members.length === 1 &&
    entry.members[0]?.resourceId === link.resourceId;
}

function entryFromLink(link: SemanticResourceLink): SemanticRelationEntry {
  return {
    key: `compat:${linkIdentity(link)}`,
    title: link.relationLabel,
    relationResourceId: link.relationResourceId ?? null,
    relationType: null,
    relationTypeLabel: null,
    groupKey: link.groupKey?.trim() || link.relationLabel,
    groupLabel: link.groupLabel?.trim() || link.relationLabel,
    displayDate: link.displayDate ?? null,
    sortDate: link.sortDate ?? null,
    documentUri: link.documentUri ?? null,
    values: [],
    members: [
      {
        resourceId: link.resourceId,
        displayName: link.displayName,
        type: link.type,
        typeLabel: link.typeLabel,
        roleLabel: null,
        documentUri: link.documentUri ?? null,
        hasDocument: link.hasDocument
      }
    ]
  };
}

function completeEntries(page: SemanticResourcePage): SemanticRelationEntry[] {
  const entries = page.entries ?? [];
  if (entries.length === 0) return [];

  const missing = availableLinks(page)
    .filter(link => !entries.some(entry => entryRepresentsLink(entry, link)))
    .map(entryFromLink);
  const seen = new Set<string>();
  return [...entries, ...missing].filter(entry => {
    if (seen.has(entry.key)) return false;
    seen.add(entry.key);
    return true;
  });
}

function withoutCurrentResource(
  entry: SemanticRelationEntry,
  currentResourceId: string
): SemanticRelationEntry | null {
  const currentMembers = entry.members.filter(member =>
    member.resourceId === currentResourceId);
  if (currentMembers.length === 0) return entry;

  const currentDocumentUris = new Set(
    currentMembers
      .map(member => member.documentUri)
      .filter((value): value is string => value !== null)
  );
  const members = entry.members.filter(member =>
    member.resourceId !== currentResourceId);
  const documentUri = entry.documentUri !== null &&
    currentDocumentUris.has(entry.documentUri)
    ? null
    : entry.documentUri;

  if (members.length === 0 && documentUri === null && entry.values.length === 0) {
    return null;
  }

  return {
    ...entry,
    members,
    documentUri
  };
}

function asTextEntry(entry: SemanticRelationEntry): SemanticRelationEntry {
  return {
    ...entry,
    documentUri: null,
    members: entry.members.map(member => ({
      ...member,
      documentUri: null,
      hasDocument: false
    }))
  };
}

function asTextLink(link: SemanticResourceLink): SemanticResourceLink {
  return {
    ...link,
    documentUri: null,
    hasDocument: false
  };
}

function publicGroupTitle(entry: SemanticRelationEntry): string {
  if (/(^|[/#])reflection$/i.test(entry.relationType ?? "")) {
    return "Отражены";
  }

  return entry.relationTypeLabel?.trim() ||
    entry.groupLabel.trim() ||
    entry.title.trim() ||
    "Связи";
}

function blocksFromEntries(entries: SemanticRelationEntry[]): SemanticContentBlockDefinition[] {
  const media = entries.filter(entryHasDocument);
  const plain = entries.filter(entry => !entryHasDocument(entry));
  const blocks: SemanticContentBlockDefinition[] = [];

  if (media.length > 0) {
    blocks.push(relationEntryBlock("public:media", "Фотографии", media));
  }

  const groups = new Map<string, { title: string; entries: SemanticRelationEntry[] }>();
  for (const entry of plain) {
    const title = publicGroupTitle(entry);
    const key = entry.relationType?.trim() || entry.groupKey.trim() || title;
    const existing = groups.get(key);
    if (existing) existing.entries.push(entry);
    else groups.set(key, { title, entries: [entry] });
  }

  for (const [key, group] of groups) {
    blocks.push(relationEntryBlock(`public:links:${key}`, group.title, group.entries));
  }
  return blocks;
}

function blocksFromLinks(
  page: SemanticResourcePage,
  textOnly: boolean
): SemanticContentBlockDefinition[] {
  const seen = new Set<string>();
  const links = availableLinks(page)
    .filter(link => {
      if (link.resourceId === page.portrait.resourceId) return false;

      const identity = linkIdentity(link);
      if (seen.has(identity)) return false;
      seen.add(identity);
      return true;
    })
    .map(link => textOnly ? asTextLink(link) : link);
  const media = links.filter(hasDocument);
  const plain = links.filter(link => !hasDocument(link));
  const blocks: SemanticContentBlockDefinition[] = [];

  if (media.length > 0) {
    blocks.push(linkBlock("public:media", "Фотографии", media));
  }

  const groups = new Map<string, { title: string; links: SemanticResourceLink[] }>();
  for (const link of plain) {
    const title = link.groupLabel?.trim() || link.relationLabel.trim() || "Связи";
    const key = link.groupKey?.trim() || title;
    const existing = groups.get(key);
    if (existing) existing.links.push(link);
    else groups.set(key, { title, links: [link] });
  }

  for (const [key, group] of groups) {
    blocks.push(linkBlock(`public:links:${key}`, group.title, group.links));
  }
  return blocks;
}

export function SemanticResourceSections({
  page,
  textOnly = false
}: SemanticResourceSectionsProps) {
  const complete = completeEntries(page);
  const entries = complete
    .map(entry => withoutCurrentResource(entry, page.portrait.resourceId))
    .filter((entry): entry is SemanticRelationEntry => entry !== null)
    .map(entry => textOnly ? asTextEntry(entry) : entry);
  const blocks = complete.length > 0
    ? blocksFromEntries(entries)
    : blocksFromLinks(page, textOnly);
  return (
    <SemanticContentBlocks
      blocks={blocks}
      currentResourceId={page.portrait.resourceId}
    />
  );
}
