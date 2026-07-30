import type {
  SemanticResourceLink,
  SemanticResourcePage
} from "../api/models";
import {
  linkBlock,
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

export function SemanticResourceSections({ page }: SemanticResourceSectionsProps) {
  return <SemanticContentBlocks blocks={relationBlocks(page)} />;
}
