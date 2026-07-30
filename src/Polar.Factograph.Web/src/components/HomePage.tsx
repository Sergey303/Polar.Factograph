import { useQueries } from "@tanstack/react-query";
import type { ProjectOverview, SemanticResourcePage } from "../api/models";
import { resourceDocumentUris } from "../app/resourceDocuments";
import { resourcePageQueryOptions } from "../app/queryOptions";
import { SearchPanel } from "./SearchPanel";
import {
  SemanticContentBlocks,
  type SemanticContentBlockDefinition
} from "./SemanticContentBlocks";

interface HomePageProps {
  project: ProjectOverview;
  token: string;
  onSearch: (query: string) => void;
}

function isNamePredicate(predicate: string): boolean {
  return /(^|[/#])(name|alias)$/i.test(predicate);
}

function titleOf(page: SemanticResourcePage): string {
  const named = page.portrait.literals.find(field => isNamePredicate(field.predicate));
  return named?.displayValue || page.portrait.resourceId;
}

function homeBlocks(pages: SemanticResourcePage[]): SemanticContentBlockDefinition[] {
  const media: SemanticContentBlockDefinition["items"] = [];
  const links: SemanticContentBlockDefinition["items"] = [];

  for (const page of pages) {
    const documents = resourceDocumentUris(page.portrait);
    const item = {
      key: `home:${page.portrait.resourceId}`,
      resourceId: page.portrait.resourceId,
      title: titleOf(page),
      members: null,
      values: [],
      sectionKey: "home",
      sectionTitle: "Коллекции сайта",
      documentUri: documents.length === 1 ? documents[0] : null,
      hasDocument: documents.length > 0,
      displayDate: null,
      sortDate: null
    };
    if (item.hasDocument) media.push(item);
    else links.push(item);
  }

  const blocks: SemanticContentBlockDefinition[] = [];
  if (media.length > 0) {
    blocks.push({
      key: "home:media",
      title: "Фотографии",
      kind: "media",
      items: media
    });
  }
  if (links.length > 0) {
    blocks.push({
      key: "home:links",
      title: "Коллекции сайта",
      kind: "text",
      items: links
    });
  }
  return blocks;
}

export function HomePage({ project, token, onSearch }: HomePageProps) {
  const resourceIds = project.homeResourceIds ?? [];
  const queries = useQueries({
    queries: resourceIds.map(resourceId => resourcePageQueryOptions(resourceId, token))
  });
  const pages = queries
    .map(query => query.data)
    .filter((page): page is SemanticResourcePage => page !== undefined);
  const loading = queries.some(query => query.isPending);
  const failed = queries.filter(query => query.isError).length;
  const blocks = homeBlocks(pages);

  return (
    <main className="page-shell home-page-shell">
      <section className="panel home-page-panel">
        <SearchPanel
          query=""
          loading={loading}
          error={failed > 0 ? `Не удалось загрузить ресурсов: ${failed}.` : null}
          onSearch={onSearch}
        />
        {blocks.length > 0 ? (
          <SemanticContentBlocks blocks={blocks} />
        ) : !loading && (
          <div className="empty-state">
            <strong>Главная страница пока не заполнена</strong>
            <span>Добавьте идентификаторы ресурсов в homeResourceIds конфигурации проекта.</span>
          </div>
        )}
      </section>
    </main>
  );
}
