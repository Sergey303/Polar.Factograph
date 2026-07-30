import { useEffect, useMemo, useRef, useState } from "react";
import { documentContentUrl } from "../../api/factographApi";
import type { DocumentVariant } from "../../api/models";
import { followAppLink, resourceHref } from "../../app/routes";
import "../../styles/semantic-content-flat.css";
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
  small: "Маленькие фотографии",
  medium: "Средние фотографии",
  large: "Большие фотографии"
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

function defaultLayout(kind: BlockKind): BlockLayout {
  return kind === "media" ? "small" : "list";
}

function layoutStorageKey(blockKey: string, timeline: boolean): string {
  return `polar-factograph:block-layout:${timeline ? "$timeline" : blockKey}`;
}

function storedLayout(
  blockKey: string,
  kind: BlockKind,
  timeline: boolean
): BlockLayout {
  const fallback = defaultLayout(kind);
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

  useEffect(() => {
    if (!layoutsFor(kind).includes(layout)) {
      setLayout(defaultLayout(kind));
    }
  }, [kind, layout]);

  function change(next: BlockLayout): void {
    setLayout(next);
    try {
      window.localStorage.setItem(layoutStorageKey(blockKey, timeline), next);
    } catch {
      // The selected view still works when local storage is unavailable.
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
  return (
    <details className="block-layout-menu">
      <summary title={`Вид: ${layoutLabels[props.layout]}`}>
        <span aria-hidden="true">{layoutIcons[props.layout]}</span>
        <span className="block-layout-current">{layoutLabels[props.layout]}</span>
      </summary>
      <div className="block-layout-options" role="menu" aria-label={`Вид блока «${props.title}»`}>
        {layoutsFor(props.kind).map(layout => (
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
    return <span className="semantic-document-glyph" aria-hidden="true">▧</span>;
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

function EntityLink(props: { resourceId: string; displayName: string }) {
  return (
    <a href={resourceHref(props.resourceId)} onClick={followAppLink}>
      {props.displayName}
    </a>
  );
}

function MediaLink(props: {
  item: SemanticContentItem;
  layout: BlockLayout;
  className: string;
}) {
  return (
    <a
      className={props.className}
      href={resourceHref(props.item.resourceId)}
      onClick={followAppLink}
      aria-label="Открыть фотографию"
    >
      <SemanticThumbnail documentUri={props.item.documentUri} layout={props.layout} />
    </a>
  );
}

function publicMembers(item: SemanticContentItem): SemanticContentMember[] {
  if (item.members === null) return [];
  return item.members.filter(member => !member.hasDocument);
}

function primaryMember(item: SemanticContentItem): SemanticContentMember | null {
  if (item.hasDocument) return null;
  return publicMembers(item)[0] ?? null;
}

function secondaryMembers(item: SemanticContentItem): SemanticContentMember[] {
  const members = publicMembers(item);
  return item.hasDocument ? members : members.slice(1);
}

function ItemFacts({ item }: { item: SemanticContentItem }) {
  const primary = primaryMember(item);
  const related = secondaryMembers(item);
  return (
    <div className="semantic-public-facts">
      {!item.hasDocument && item.members === null && (
        <strong><EntityLink resourceId={item.resourceId} displayName={item.title} /></strong>
      )}
      {primary && (
        <strong><EntityLink resourceId={primary.resourceId} displayName={primary.displayName} /></strong>
      )}
      {related.map(member => (
        <EntityLink
          key={member.resourceId}
          resourceId={member.resourceId}
          displayName={member.displayName}
        />
      ))}
      {item.values.map((value, index) => (
        <span key={`${value}:${index}`}>{value}</span>
      ))}
      {item.displayDate && <time>{item.displayDate}</time>}
    </div>
  );
}

interface FlatTableRow {
  key: string;
  item: SemanticContentItem;
  related: SemanticContentMember | null;
  value: string | null;
  first: boolean;
}

function flattenTableRows(items: SemanticContentItem[]): FlatTableRow[] {
  const rows: FlatTableRow[] = [];
  for (const item of items) {
    const related = secondaryMembers(item);
    const count = Math.max(1, related.length, item.values.length);
    for (let index = 0; index < count; index += 1) {
      rows.push({
        key: `${item.key}:${index}`,
        item,
        related: related[index] ?? null,
        value: item.values[index] ?? null,
        first: index === 0
      });
    }
  }
  return rows;
}

interface TableColumns {
  related: boolean;
  values: boolean;
  date: boolean;
}

function tableColumns(items: SemanticContentItem[]): TableColumns {
  return {
    related: items.some(item => secondaryMembers(item).length > 0),
    values: items.some(item => item.values.length > 0),
    date: items.some(item => item.displayDate !== null)
  };
}

function TableMaterial({ row }: { row: FlatTableRow }) {
  if (!row.first) return null;
  if (row.item.hasDocument) {
    return <MediaLink item={row.item} layout="table" className="semantic-table-material" />;
  }
  const primary = primaryMember(row.item);
  if (primary) {
    return <EntityLink resourceId={primary.resourceId} displayName={primary.displayName} />;
  }
  return <EntityLink resourceId={row.item.resourceId} displayName={row.item.title} />;
}

function TableItems(props: {
  items: SemanticContentItem[];
  hideHeader?: boolean;
  columns?: TableColumns;
}) {
  const rows = flattenTableRows(props.items);
  const columns = props.columns ?? tableColumns(props.items);
  return (
    <div className="semantic-table-wrap">
      <table className="semantic-content-table semantic-public-table">
        {!props.hideHeader && (
          <thead>
            <tr>
              <th>Материал</th>
              {columns.related && <th>Связано с</th>}
              {columns.values && <th>Сведения</th>}
              {columns.date && <th>Дата</th>}
            </tr>
          </thead>
        )}
        <tbody>
          {rows.map(row => (
            <tr key={row.key}>
              <td className="semantic-material-cell"><TableMaterial row={row} /></td>
              {columns.related && (
                <td>
                  {row.related && (
                    <EntityLink
                      resourceId={row.related.resourceId}
                      displayName={row.related.displayName}
                    />
                  )}
                </td>
              )}
              {columns.values && <td>{row.value}</td>}
              {columns.date && <td className="semantic-date-cell">{row.first ? row.item.displayDate : null}</td>}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ListItems({ items }: { items: SemanticContentItem[] }) {
  return (
    <div className="semantic-content-list semantic-public-list">
      {items.map(item => (
        <article
          className={`semantic-content-list-item ${item.hasDocument ? "has-media" : "text-only"}`}
          key={item.key}
        >
          {item.hasDocument && (
            <MediaLink item={item} layout="small" className="semantic-list-preview" />
          )}
          <ItemFacts item={item} />
        </article>
      ))}
    </div>
  );
}

function IconItems(props: {
  items: SemanticContentItem[];
  layout: "small" | "medium" | "large";
}) {
  return (
    <div className={`semantic-icon-grid semantic-icon-grid-${props.layout} semantic-public-grid`}>
      {props.items.map(item => (
        <article
          className={`semantic-icon-card ${item.hasDocument ? "has-media" : "text-only"}`}
          key={item.key}
        >
          {item.hasDocument && (
            <MediaLink item={item} layout={props.layout} className="semantic-icon-preview" />
          )}
          <ItemFacts item={item} />
        </article>
      ))}
    </div>
  );
}

function BlockItems(props: {
  items: SemanticContentItem[];
  layout: BlockLayout;
  hideTableHeader?: boolean;
  columns?: TableColumns;
}) {
  if (props.items.length === 0) return null;
  if (props.layout === "table") {
    return (
      <TableItems
        items={props.items}
        hideHeader={props.hideTableHeader}
        columns={props.columns}
      />
    );
  }
  if (props.layout === "small" || props.layout === "medium" || props.layout === "large") {
    return <IconItems items={props.items} layout={props.layout} />;
  }
  return <ListItems items={props.items} />;
}

function pageSize(layout: BlockLayout): number {
  switch (layout) {
    case "table": return 30;
    case "small": return 30;
    case "medium": return 18;
    case "large": return 10;
    default: return 18;
  }
}

function Pagination(props: {
  page: number;
  pageCount: number;
  from: number;
  to: number;
  total: number;
  onChange: (page: number) => void;
}) {
  if (props.pageCount <= 1) return null;
  return (
    <nav className="semantic-block-pagination" aria-label="Страницы материалов">
      <button
        className="button ghost compact"
        type="button"
        disabled={props.page === 0}
        onClick={() => props.onChange(Math.max(0, props.page - 1))}
      >
        Предыдущие
      </button>
      <span>{props.from + 1}–{props.to} из {props.total}</span>
      <button
        className="button ghost compact"
        type="button"
        disabled={props.page >= props.pageCount - 1}
        onClick={() => props.onChange(Math.min(props.pageCount - 1, props.page + 1))}
      >
        Следующие
      </button>
    </nav>
  );
}

function PagedBlockBody(props: {
  block: SemanticContentBlockDefinition;
  layout: BlockLayout;
  timeline: boolean;
}) {
  const [page, setPage] = useState(0);
  const size = pageSize(props.layout);
  const pageCount = Math.max(1, Math.ceil(props.block.items.length / size));
  const currentPage = Math.min(page, pageCount - 1);
  const from = currentPage * size;
  const visible = props.block.items.slice(from, from + size);
  const firstUndated = props.timeline
    ? visible.findIndex(item => item.sortDate === null)
    : -1;
  const dated = firstUndated < 0 ? visible : visible.slice(0, firstUndated);
  const undated = firstUndated < 0 ? [] : visible.slice(firstUndated);
  const columns = useMemo(() => tableColumns(visible), [visible]);

  useEffect(() => {
    setPage(0);
  }, [props.layout, props.block.key]);

  return (
    <>
      <BlockItems items={dated} layout={props.layout} columns={columns} />
      {undated.length > 0 && (
        <>
          <div className="timeline-undated-label">Без указанной даты</div>
          <BlockItems
            items={undated}
            layout={props.layout}
            hideTableHeader={dated.length > 0}
            columns={columns}
          />
        </>
      )}
      <Pagination
        page={currentPage}
        pageCount={pageCount}
        from={from}
        to={Math.min(from + size, props.block.items.length)}
        total={props.block.items.length}
        onChange={setPage}
      />
    </>
  );
}

function SemanticContentBlock(props: {
  block: SemanticContentBlockDefinition;
  timeline?: boolean;
}) {
  const timeline = props.timeline === true;
  const layoutState = useBlockLayout(props.block.key, props.block.kind, timeline);
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
      <PagedBlockBody
        block={props.block}
        layout={layoutState.layout}
        timeline={timeline}
      />
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
  return left.title.localeCompare(right.title, "ru") || left.key.localeCompare(right.key, "ru");
}

function SectionsMenu(props: {
  blocks: SemanticContentBlockDefinition[];
  selected: Set<string>;
  onChange: (selected: Set<string>) => void;
}) {
  return (
    <details className="semantic-sections-menu">
      <summary>Разделы: {props.selected.size} из {props.blocks.length}</summary>
      <div className="semantic-sections-popover">
        {props.blocks.map(block => (
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

function prepareBlocks(
  blocks: SemanticContentBlockDefinition[],
  currentResourceId: string | undefined
): SemanticContentBlockDefinition[] {
  if (!currentResourceId) return blocks;
  return blocks.map(block => {
    const items: SemanticContentItem[] = [];
    for (const item of block.items) {
      if (item.members === null) {
        items.push(item);
        continue;
      }
      const members = item.members.filter(member => member.resourceId !== currentResourceId);
      if (members.length === 0 && !item.hasDocument) continue;
      let next = { ...item, members };
      if (item.resourceId === currentResourceId) {
        const replacement = members.find(member => member.hasDocument) ?? members[0];
        if (!replacement) continue;
        next = {
          ...next,
          resourceId: replacement.resourceId,
          documentUri: replacement.documentUri,
          hasDocument: replacement.hasDocument
        };
      }
      items.push(next);
    }
    return { ...block, items };
  });
}

export function SemanticContentBlocks({
  blocks,
  currentResourceId
}: SemanticContentBlocksProps) {
  const prepared = useMemo(
    () => prepareBlocks(blocks, currentResourceId),
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

  if (nonEmpty.length === 0) return null;

  const visibleBlocks = nonEmpty.filter(block => selected.has(block.key));
  const timelineItems = visibleBlocks.flatMap(block => block.items).sort(timelineSort);
  const timelineBlock: SemanticContentBlockDefinition = {
    key: "$timeline",
    title: "Хронология",
    kind: timelineItems.some(item => item.hasDocument) ? "media" : "text",
    items: timelineItems
  };

  return (
    <div className="semantic-content-sections semantic-public-content">
      <div className="semantic-page-toolbar">
        <label className="timeline-toggle">
          <input
            type="checkbox"
            checked={timeline}
            onChange={event => setTimeline(event.target.checked)}
          />
          <span>Хронология</span>
        </label>
        {nonEmpty.length > 1 && (
          <SectionsMenu blocks={nonEmpty} selected={selected} onChange={setSelected} />
        )}
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
