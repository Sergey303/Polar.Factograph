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

function blocksFromEntries(entries: SemanticRelationEntry[]): SemanticContentBlockDefinition[] {
  const media = entries.filter(entryHasDocument);
  const links = entries.filter(entry => !entryHasDocument(entry));
  const blocks: SemanticContentBlockDefinition[] = [];
  if (media.length > 0) {
    blocks.push(relationEntryBlock("public:media", "Фотографии", media));
  }
  if (links.length > 0) {
    blocks.push(relationEntryBlock("public:links", "Связи", links));
  }
  return blocks;
}

function blocksFromLinks(page: SemanticResourcePage): SemanticContentBlockDefinition[] {
  const seen = new Set<string>();
  const links = availableLinks(page).filter(link => {
    const identity = linkIdentity(link);
    if (seen.has(identity)) return false;
    seen.add(identity);
    return true;
  });
  const media = links.filter(hasDocument);
  const plain = links.filter(link => !hasDocument(link));
  const blocks: SemanticContentBlockDefinition[] = [];
  if (media.length > 0) blocks.push(linkBlock("public:media", "Фотографии", media));
  if (plain.length > 0) blocks.push(linkBlock("public:links", "Связи", plain));
  return blocks;
}

export function SemanticResourceSections({ page }: SemanticResourceSectionsProps) {
  const entries = completeEntries(page);
  const blocks = entries.length > 0
    ? blocksFromEntries(entries)
    : blocksFromLinks(page);
  return (
    <SemanticContentBlocks
      blocks={blocks}
      currentResourceId={page.portrait.resourceId}
    />
  );
}
