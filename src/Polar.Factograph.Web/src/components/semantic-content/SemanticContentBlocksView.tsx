import { useEffect, useMemo, useRef, useState } from "react";
import { documentContentUrl } from "../../api/factographApi";
import type { DocumentVariant } from "../../api/models";
import { followAppLink, resourceHref } from "../../app/routes";
import "../../styles/semantic-content-flat.css";
import {
  PageVirtualizedChunks,
  type PageVirtualizedChunkDefinition
} from "../PageVirtualizedChunks";
import type {
  BlockKind,
  BlockLayout,
  SemanticContentBlockDefinition,
  SemanticContentItem,
  SemanticContentMember
} from "./model";

interface SemanticContentBlocksProps {
  blocks: SemanticContentBlockDefinition[];
  currentResourceId?: string;
}

const layoutLabels: Record<BlockLayout, string> = {
  list: "Список",
  table: "Таблица",
  small: "Маленькие значки",
  medium: "Средние значки",
  large: "Большие значки"
};

const layoutIcons: Record<BlockLayout, string> = {
  list: "☰",
  table: "▤",
  small: "▪",
  medium: "▦",
  large: "▩"
};

function layoutsFor(kind: BlockKind): BlockLayout[] {
  return kind === "text"
    ? ["list", "table"]
    : ["list", "table", "small", "medium", "large"];
}

function defaultLayout(kind: BlockKind, timeline: boolean): BlockLayout {
  if (timeline || kind === "text") return "list";
  return "small";
}

function layoutStorageKey(blockKey: string, timeline: boolean): string {
  return `polar-factograph:block-layout:${timeline ? "$timeline" : blockKey}`;
}

function storedLayout(
  blockKey: string,
  kind: BlockKind,
  timeline: boolean
): BlockLayout {
  const fallback = defaultLayout(kind, timeline);
  try {
    const value = window.localStorage.getItem(layoutStorageKey(blockKey, timeline));
    return layoutsFor(kind).includes(value as BlockLayout)
      ? value as BlockLayout
      : fallback;
  } catch {
    return fallback;
  }
}

function useBlockLayout(blockKey: string, kind: BlockKind, timeline: boolean) {
  const [layout, setLayout] = useState<BlockLayout>(() =>
    storedLayout(blockKey, kind, timeline));

  function change(next: BlockLayout): void {
    setLayout(next);
    try {
      window.localStorage.setItem(layoutStorageKey(blockKey, timeline), next);
    } catch {
      // A private browser session may reject local storage; the view still changes.
    }
  }

  return { layout, change };
}

function BlockLayoutMenu(props: {
  title: string;
  kind: BlockKind;
  layout: BlockLayout;
  onChange: (layout: BlockLayout) => void;
}) {
  const available = layoutsFor(props.kind);
  return (
    <details className="block-layout-menu">
      <summary title={`Вид: ${layoutLabels[props.layout]}`}>
        <span aria-hidden="true">{layoutIcons[props.layout]}</span>
        <span className="block-layout-current">{layoutLabels[props.layout]}</span>
      </summary>
      <div className="block-layout-options" role="menu" aria-label={`Вид блока «${props.title}»`}>
        {available.map(layout => (
          <button
            key={layout}
            className={layout === props.layout ? "selected" : ""}
            type="button"
            role="menuitemradio"
            aria-checked={layout === props.layout}
            onClick={event => {
              props.onChange(layout);
              event.currentTarget.closest("details")?.removeAttribute("open");
            }}
          >
            <span aria-hidden="true">{layoutIcons[layout]}</span>
            {layoutLabels[layout]}
          </button>
        ))}
      </div>
    </details>
  );
}

function previewVariant(layout: BlockLayout): DocumentVariant {
  switch (layout) {
    case "table":
      return "icon";
    case "medium":
      return "medium";
    case "large":
      return "normal";
    default:
      return "small";
  }
}

function SemanticThumbnail(props: {
  documentUri: string | null;
  layout: BlockLayout;
}) {
  const source = props.documentUri === null
    ? null
    : documentContentUrl(props.documentUri, previewVariant(props.layout));
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    setFailed(false);
  }, [source]);

  if (source === null || failed) {
    return <span className="semantic-item-placeholder" aria-hidden="true">◇</span>;
  }

  return (
    <img
      src={source}
      alt=""
      loading="lazy"
      onError={() => setFailed(true)}
    />
  );
}

function ResourceLink(props: {
  resourceId: string;
  displayName: string;
  documentUri: string | null;
}) {
  const documentBacked = props.documentUri !== null;
  return (
    <a
      href={resourceHref(props.resourceId)}
      onClick={followAppLink}
      aria-label={documentBacked ? "Открыть связанный документ" : undefined}
    >
      {documentBacked ? "Открыть" : props.displayName}
    </a>
  );
}

function RelationMembers({ members }: { members: SemanticContentMember[] }) {
  return (
    <div className="semantic-relation-members">
      {members.map((member, index) => (
        <span
          className="semantic-relation-member"
          key={`${member.resourceId}:${member.roleLabel ?? ""}:${index}`}
        >
          {member.roleLabel && <small>{member.roleLabel}</small>}
          <ResourceLink
            resourceId={member.resourceId}
            displayName={member.displayName}
            documentUri={member.documentUri}
          />
        </span>
      ))}
    </div>
  );
}

function ItemName(props: {
  item: SemanticContentItem;
  showSection: boolean;
}) {
  const relation = props.item.members !== null;
  return (
    <div className="semantic-item-name">
      {relation ? (
        props.showSection && (
          <>
            <strong className="semantic-relation-title">{props.item.title}</strong>
            {props.item.detail && <span>{props.item.detail}</span>}
          </>
        )
      ) : (
        <>
          <ResourceLink
            resourceId={props.item.resourceId}
            displayName={props.item.title}
            documentUri={props.item.documentUri}
          />
          {props.showSection && props.item.relationLabel && (
            <span>{props.item.relationLabel}</span>
          )}
          {props.item.typeLabel && <span>{props.item.typeLabel}</span>}
          {props.item.detail && <span>{props.item.detail}</span>}
        </>
      )}
      {props.item.members && <RelationMembers members={props.item.members} />}
      {props.showSection && <small>{props.item.sectionTitle}</small>}
    </div>
  );
}

interface FlatTableRow {
  key: string;
  item: SemanticContentItem;
  member: SemanticContentMember | null;
}

function flattenTableRows(items: SemanticContentItem[]): FlatTableRow[] {
  return items.flatMap(item => {
    if (item.members === null || item.members.length === 0) {
      return [{ key: item.key, item, member: null }];
    }

    return item.members.map((member, index) => ({
      key: `${item.key}:${member.resourceId}:${member.roleLabel ?? ""}:${index}`,
      item,
      member
    }));
  });
}

function rowDocumentUri(row: FlatTableRow): string | null {
  return row.member === null ? row.item.documentUri : row.member.documentUri;
}

function rowResourceId(row: FlatTableRow): string {
  return row.member?.resourceId ?? row.item.resourceId;
}

function rowDisplayName(row: FlatTableRow): string {
  return row.member?.displayName ?? row.item.title;
}

function rowRelation(row: FlatTableRow, showSection: boolean): string | null {
  if (!showSection) return null;
  return row.item.members === null
    ? row.item.relationLabel
    : row.item.title;
}

function rowType(row: FlatTableRow, showSection: boolean): string | null {
  if (row.item.members !== null && !showSection) return null;
  return row.item.typeLabel;
}

function TableItems(props: {
  items: SemanticContentItem[];
  showSection: boolean;
  hideTableHeader?: boolean;
}) {
  const rows = flattenTableRows(props.items);
  const showMedia = rows.some(row => rowDocumentUri(row) !== null);
  const showRelation = rows.some(row => rowRelation(row, props.showSection) !== null);
  const showType = rows.some(row => rowType(row, props.showSection) !== null);
  const showRole = rows.some(row => row.member?.roleLabel != null);

  return (
    <div className="semantic-table-wrap">
      <table className="semantic-content-table semantic-content-table-flat">
        {!props.hideTableHeader && (
          <thead>
            <tr>
              {showMedia && (
                <th className="semantic-media-column">
                  <span className="visually-hidden">Изображение</span>
                </th>
              )}
              {showRelation && <th className="semantic-relation-column">Связь</th>}
              {showType && <th className="semantic-type-column">Тип</th>}
              {showRole && <th className="semantic-role-column">Роль</th>}
              <th>Объект</th>
              {props.showSection && <th className="semantic-section-column">Раздел</th>}
              <th className="semantic-date-cell">Дата</th>
            </tr>
          </thead>
        )}
        <tbody>
          {rows.map(row => {
            const documentUri = rowDocumentUri(row);
            return (
              <tr key={row.key}>
                {showMedia && (
                  <td className="semantic-media-column">
                    {documentUri === null ? (
                      <span><SemanticThumbnail documentUri={null} layout="table" /></span>
                    ) : (
                      <a
                        href={resourceHref(rowResourceId(row))}
                        onClick={followAppLink}
                        tabIndex={-1}
                        aria-label="Открыть связанный документ"
                      >
                        <SemanticThumbnail documentUri={documentUri} layout="table" />
                      </a>
                    )}
                  </td>
                )}
                {showRelation && (
                  <td className="semantic-relation-cell">
                    {rowRelation(row, props.showSection) ?? "—"}
                  </td>
                )}
                {showType && (
                  <td className="semantic-type-cell">
                    {rowType(row, props.showSection) ?? "—"}
                  </td>
                )}
                {showRole && (
                  <td className="semantic-role-cell">{row.member?.roleLabel ?? "—"}</td>
                )}
                <td className="semantic-object-cell">
                  <ResourceLink
                    resourceId={rowResourceId(row)}
                    displayName={rowDisplayName(row)}
                    documentUri={documentUri}
                  />
                </td>
                {props.showSection && (
                  <td className="semantic-section-cell">{row.item.sectionTitle}</td>
                )}
                <td className="semantic-date-cell">{row.item.displayDate ?? "—"}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function BlockItems(props: {
  items: SemanticContentItem[];
  layout: BlockLayout;
  showSection: boolean;
  hideTableHeader?: boolean;
}) {
  if (props.items.length === 0) return null;

  if (props.layout === "table") {
    return (
      <TableItems
        items={props.items}
        showSection={props.showSection}
        hideTableHeader={props.hideTableHeader}
      />
    );
  }

  if (props.layout === "small" || props.layout === "medium" || props.layout === "large") {
    return (
      <div className={`semantic-icon-grid semantic-icon-grid-${props.layout}`}>
        {props.items.map(item => (
          <article className="semantic-icon-card" key={item.key}>
            <a
              className="semantic-icon-preview"
              href={resourceHref(item.resourceId)}
              onClick={followAppLink}
              aria-label={item.documentUri === null
                ? `Открыть ${item.title}`
                : "Открыть связанный документ"}
            >
              <SemanticThumbnail documentUri={item.documentUri} layout={props.layout} />
            </a>
            <ItemName item={item} showSection={props.showSection} />
            {item.displayDate && <time>{item.displayDate}</time>}
          </article>
        ))}
      </div>
    );
  }

  return (
    <div className="semantic-content-list">
      {props.items.map(item => (
        <article className="semantic-content-list-item" key={item.key}>
          {item.documentUri !== null && (
            <a
              className="semantic-list-preview"
              href={resourceHref(item.resourceId)}
              onClick={followAppLink}
              tabIndex={-1}
              aria-label="Открыть связанный документ"
            >
              <SemanticThumbnail documentUri={item.documentUri} layout="small" />
            </a>
          )}
          <div>
            {item.displayDate && <time>{item.displayDate}</time>}
            <ItemName item={item} showSection={props.showSection} />
          </div>
        </article>
      ))}
    </div>
  );
}

function pageSize(layout: BlockLayout): number {
  switch (layout) {
    case "table":
      return 20;
    case "small":
      return 24;
    case "medium":
      return 12;
    case "large":
      return 6;
    default:
      return 12;
  }
}

function timelineChunkSize(layout: BlockLayout): number {
  switch (layout) {
    case "table":
      return 40;
    case "medium":
      return 12;
    case "large":
      return 6;
    default:
      return 24;
  }
}

function estimatedTimelineItemHeight(layout: BlockLayout): number {
  switch (layout) {
    case "table":
      return 52;
    case "small":
      return 180;
    case "medium":
      return 260;
    case "large":
      return 380;
    default:
      return 78;
  }
}

function TimelineItemSequence(props: {
  items: SemanticContentItem[];
  layout: BlockLayout;
  hideFirstTableHeader?: boolean;
  eagerFirst?: boolean;
}) {
  const chunks = useMemo<PageVirtualizedChunkDefinition[]>(() => {
    const size = timelineChunkSize(props.layout);
    const estimate = estimatedTimelineItemHeight(props.layout);
    const result: PageVirtualizedChunkDefinition[] = [];
    for (let from = 0; from < props.items.length; from += size) {
      const items = props.items.slice(from, from + size);
      const hideTableHeader = props.layout === "table" &&
        (props.hideFirstTableHeader === true || from > 0);
      const contentKey = items.map(item => item.key).join("\n");
      result.push({
        key: `${props.layout}:${contentKey}`,
        estimatedHeight: items.length * estimate +
          (props.layout === "table" && !hideTableHeader ? 34 : 0),
        eager: props.eagerFirst === true && from === 0,
        content: (
          <BlockItems
            items={items}
            layout={props.layout}
            showSection
            hideTableHeader={hideTableHeader}
          />
        )
      });
    }
    return result;
  }, [props.eagerFirst, props.hideFirstTableHeader, props.items, props.layout]);

  return <PageVirtualizedChunks chunks={chunks} />;
}

function TimelineBlockBody(props: {
  items: SemanticContentItem[];
  layout: BlockLayout;
}) {
  const firstUndated = props.items.findIndex(item => item.sortDate === null);
  const dated = firstUndated < 0 ? props.items : props.items.slice(0, firstUndated);
  const undated = firstUndated < 0 ? [] : props.items.slice(firstUndated);

  return (
    <>
      <TimelineItemSequence
        items={dated}
        layout={props.layout}
        eagerFirst
      />
      {undated.length > 0 && (
        <>
          <div className="timeline-undated-label">Без указанной даты</div>
          <TimelineItemSequence
            items={undated}
            layout={props.layout}
            hideFirstTableHeader={dated.length > 0}
            eagerFirst={dated.length === 0}
          />
        </>
      )}
    </>
  );
}

function GroupedBlockBody(props: {
  block: SemanticContentBlockDefinition;
  layout: BlockLayout;
}) {
  const [page, setPage] = useState(0);
  const size = pageSize(props.layout);
  const pageCount = Math.max(1, Math.ceil(props.block.items.length / size));
  const currentPage = Math.min(page, pageCount - 1);
  const from = currentPage * size;
  const visible = props.block.items.slice(from, from + size);

  useEffect(() => {
    setPage(0);
  }, [props.layout, props.block.key]);

  return (
    <>
      <BlockItems items={visible} layout={props.layout} showSection={false} />
      {pageCount > 1 && (
        <nav className="semantic-block-pagination" aria-label={`Страницы блока «${props.block.title}»`}>
          <button
            className="button ghost compact"
            type="button"
            disabled={currentPage === 0}
            onClick={() => setPage(value => Math.max(0, value - 1))}
          >
            Предыдущие
          </button>
          <span>{from + 1}–{Math.min(from + size, props.block.items.length)} из {props.block.items.length}</span>
          <button
            className="button ghost compact"
            type="button"
            disabled={currentPage >= pageCount - 1}
            onClick={() => setPage(value => Math.min(pageCount - 1, value + 1))}
          >
            Следующие
          </button>
        </nav>
      )}
    </>
  );
}

function SemanticContentBlock(props: {
  block: SemanticContentBlockDefinition;
  timeline?: boolean;
}) {
  const timeline = props.timeline === true;
  const layoutState = useBlockLayout(
    props.block.key,
    props.block.kind,
    timeline);

  return (
    <section className={`semantic-content-block ${timeline ? "timeline-block" : ""}`}>
      <header className="semantic-content-block-header">
        <div>
          <h2>{props.block.title}</h2>
          <span>{props.block.items.length}</span>
        </div>
        <BlockLayoutMenu
          title={props.block.title}
          kind={props.block.kind}
          layout={layoutState.layout}
          onChange={layoutState.change}
        />
      </header>

      {timeline ? (
        <TimelineBlockBody items={props.block.items} layout={layoutState.layout} />
      ) : (
        <GroupedBlockBody block={props.block} layout={layoutState.layout} />
      )}
    </section>
  );
}

function timelineSort(left: SemanticContentItem, right: SemanticContentItem): number {
  if (left.sortDate === null && right.sortDate !== null) return 1;
  if (left.sortDate !== null && right.sortDate === null) return -1;
  if (left.sortDate !== null && right.sortDate !== null) {
    const byDate = left.sortDate.localeCompare(right.sortDate, "ru");
    if (byDate !== 0) return byDate;
  }
  return left.sectionTitle.localeCompare(right.sectionTitle, "ru") ||
    left.title.localeCompare(right.title, "ru") ||
    left.key.localeCompare(right.key, "ru");
}

function SectionsMenu(props: {
  blocks: SemanticContentBlockDefinition[];
  selected: Set<string>;
  onChange: (selected: Set<string>) => void;
}) {
  const [query, setQuery] = useState("");
  const allSelected = props.selected.size === props.blocks.length;
  const normalizedQuery = query.trim().toLocaleLowerCase("ru-RU");
  const visibleBlocks = normalizedQuery.length === 0
    ? props.blocks
    : props.blocks.filter(block =>
        block.title.toLocaleLowerCase("ru-RU").includes(normalizedQuery));
  const searchable = props.blocks.length >= 8;

  return (
    <details
      className="semantic-sections-menu"
      onToggle={event => {
        if (!event.currentTarget.open) setQuery("");
      }}
    >
      <summary>
        {allSelected
          ? `Разделы: все ${props.blocks.length}`
          : `Разделы: ${props.selected.size} из ${props.blocks.length}`}
      </summary>
      <div className="semantic-sections-popover">
        <div className="semantic-sections-actions">
          <button
            type="button"
            onClick={() => props.onChange(new Set(props.blocks.map(block => block.key)))}
          >
            Выбрать все
          </button>
          <button type="button" onClick={() => props.onChange(new Set())}>
            Снять все
          </button>
        </div>
        {searchable && (
          <label className="semantic-sections-search">
            <span className="visually-hidden">Найти раздел</span>
            <input
              type="search"
              value={query}
              autoComplete="off"
              placeholder="Найти раздел…"
              onChange={event => setQuery(event.target.value)}
            />
          </label>
        )}
        {visibleBlocks.length === 0 ? (
          <div className="semantic-sections-no-results">Разделы не найдены</div>
        ) : visibleBlocks.map(block => (
          <label key={block.key}>
            <input
              type="checkbox"
              checked={props.selected.has(block.key)}
              onChange={event => {
                const next = new Set(props.selected);
                if (event.target.checked) next.add(block.key);
                else next.delete(block.key);
                props.onChange(next);
              }}
            />
            <span>{block.title}</span>
            <small>{block.items.length}</small>
          </label>
        ))}
      </div>
    </details>
  );
}

function hideCurrentEntity(
  blocks: SemanticContentBlockDefinition[],
  currentResourceId: string | undefined
): SemanticContentBlockDefinition[] {
  if (!currentResourceId) return blocks;

  return blocks.map(block => ({
    ...block,
    items: block.items.flatMap(item => {
      if (item.members === null) return [item];

      const members = item.members.filter(member =>
        member.resourceId !== currentResourceId);
      if (members.length === 0) return [];
      if (members.length === item.members.length) return [item];

      if (item.resourceId !== currentResourceId) {
        return [{ ...item, members }];
      }

      const preview = members.find(member => member.documentUri === item.documentUri) ??
        members.find(member => member.documentUri !== null) ??
        members[0];
      return [{
        ...item,
        resourceId: preview.resourceId,
        documentUri: preview.documentUri,
        members
      }];
    })
  }));
}

export function SemanticContentBlocks({
  blocks,
  currentResourceId
}: SemanticContentBlocksProps) {
  const prepared = useMemo(
    () => hideCurrentEntity(blocks, currentResourceId),
    [blocks, currentResourceId]
  );
  const nonEmpty = useMemo(
    () => prepared.filter(block => block.items.length > 0),
    [prepared]
  );
  const [timeline, setTimeline] = useState(true);
  const [selected, setSelected] = useState<Set<string>>(
    () => new Set(nonEmpty.map(block => block.key))
  );
  const previousKeys = useRef(nonEmpty.map(block => block.key).join("\n"));

  useEffect(() => {
    const keys = nonEmpty.map(block => block.key);
    const fingerprint = keys.join("\n");
    if (fingerprint === previousKeys.current) return;
    previousKeys.current = fingerprint;
    setSelected(new Set(keys));
  }, [nonEmpty]);

  const visibleBlocks = nonEmpty.filter(block => selected.has(block.key));
  const timelineItems = visibleBlocks
    .flatMap(block => block.items)
    .sort(timelineSort);
  const timelineBlock: SemanticContentBlockDefinition = {
    key: "$timeline",
    title: "Хронология",
    kind: timelineItems.some(item => item.documentUri !== null) ? "media" : "text",
    items: timelineItems
  };

  if (nonEmpty.length === 0) return null;

  return (
    <div className="semantic-content-sections">
      <div className="semantic-page-toolbar">
        <label className="timeline-toggle">
          <input
            type="checkbox"
            checked={timeline}
            onChange={event => setTimeline(event.target.checked)}
          />
          <span>Хронология</span>
        </label>
        <SectionsMenu
          blocks={nonEmpty}
          selected={selected}
          onChange={setSelected}
        />
      </div>

      {visibleBlocks.length === 0 ? (
        <div className="empty-state semantic-sections-empty">
          <strong>Все разделы скрыты</strong>
          <button
            className="button subtle compact"
            type="button"
            onClick={() => setSelected(new Set(nonEmpty.map(block => block.key)))}
          >
            Показать всё
          </button>
        </div>
      ) : timeline ? (
        <SemanticContentBlock block={timelineBlock} timeline />
      ) : (
        visibleBlocks.map(block => (
          <SemanticContentBlock key={block.key} block={block} />
        ))
      )}
    </div>
  );
}
