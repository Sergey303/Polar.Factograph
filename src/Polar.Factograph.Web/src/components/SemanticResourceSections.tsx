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

interface RelationGroup {
  key: string;
  title: string;
  links: SemanticResourceLink[];
}

interface RelationEntryGroup {
  key: string;
  title: string;
  entries: SemanticRelationEntry[];
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

function relationBlocks(page: SemanticResourcePage): SemanticContentBlockDefinition[] {
  const groups = new Map<string, RelationGroup>();
  const seen = new Set<string>();

  for (const link of availableLinks(page)) {
    const identity = linkIdentity(link);
    if (seen.has(identity)) continue;
    seen.add(identity);

    const key = link.groupKey?.trim() || link.relationLabel;
    const title = link.groupLabel?.trim() || link.relationLabel;
    const existing = groups.get(key);
    if (existing) {
      existing.links.push(link);
    } else {
      groups.set(key, { key, title, links: [link] });
    }
  }

  return [...groups.values()]
    .sort((left, right) => left.title.localeCompare(right.title, "ru"))
    .map(group => linkBlock(`relation:${group.key}`, group.title, group.links));
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
    entry.members[0]?.resourceId === link.resourceId &&
    (entry.title === link.relationLabel || entry.groupLabel === link.relationLabel);
}

function entryFromLink(link: SemanticResourceLink): SemanticRelationEntry {
  const groupKey = link.groupKey?.trim() || link.relationLabel;
  const groupLabel = link.groupLabel?.trim() || link.relationLabel;
  return {
    key: `compat:${linkIdentity(link)}`,
    title: link.relationLabel,
    relationResourceId: link.relationResourceId ?? null,
    relationType: null,
    relationTypeLabel: null,
    groupKey,
    groupLabel,
    displayDate: link.displayDate ?? null,
    sortDate: link.sortDate ?? null,
    documentUri: link.documentUri ?? null,
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
  return [...entries, ...missing];
}

function relationEntryBlocks(
  entries: SemanticRelationEntry[]
): SemanticContentBlockDefinition[] {
  const groups = new Map<string, RelationEntryGroup>();
  const seen = new Set<string>();

  for (const entry of entries) {
    if (seen.has(entry.key)) continue;
    seen.add(entry.key);

    const key = entry.groupKey.trim() || entry.key;
    const title = entry.groupLabel.trim() || entry.title;
    const existing = groups.get(key);
    if (existing) {
      existing.entries.push(entry);
    } else {
      groups.set(key, { key, title, entries: [entry] });
    }
  }

  return [...groups.values()]
    .sort((left, right) => left.title.localeCompare(right.title, "ru"))
    .map(group => relationEntryBlock(
      `relation:${group.key}`,
      group.title,
      group.entries));
}

export function SemanticResourceSections({ page }: SemanticResourceSectionsProps) {
  const entries = completeEntries(page);
  const blocks = entries.length > 0
    ? relationEntryBlocks(entries)
    : relationBlocks(page);
  return (
    <SemanticContentBlocks
      blocks={blocks}
      currentResourceId={page.portrait.resourceId}
    />
  );
}
