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

function relationBlocks(page: SemanticResourcePage): SemanticContentBlockDefinition[] {
  const groups = new Map<string, RelationGroup>();
  const seen = new Set<string>();
  const links = page.links ?? legacyLinks(page);

  for (const link of links) {
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
  const entries = page.entries ?? [];
  const blocks = entries.length > 0
    ? relationEntryBlocks(entries)
    : relationBlocks(page);
  return <SemanticContentBlocks blocks={blocks} />;
}
