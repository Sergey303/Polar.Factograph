import { useEffect, useMemo, useRef, useState } from "react";
import { documentContentUrl } from "../api/factographApi";
import type {
  DocumentVariant,
  SemanticPhotoCard,
  SemanticResourceLink
} from "../api/models";
import { followAppLink, resourceHref } from "../app/routes";

type BlockLayout = "list" | "table" | "small" | "medium" | "large";
type BlockKind = "media" | "text";

interface SemanticContentItem {
  key: string;
  resourceId: string;
  title: string;
  detail: string | null;
  sectionKey: string;
  sectionTitle: string;
  documentUri: string | null;
  displayDate: string | null;
  sortDate: string | null;
}

export interface SemanticContentBlockDefinition {
  key: string;
  title: string;
  kind: BlockKind;
  items: SemanticContentItem[];
}

interface SemanticContentBlocksProps {
  blocks: SemanticContentBlockDefinition[];
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

function layoutsFor(kind: BlockKind, timeline: boolean): BlockLayout[] {
  if (timeline || kind === "text") return ["list", "table"];
  return ["list", "table", "small", "medium", "large"];
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
    return layoutsFor(kind, timeline).includes(value as BlockLayout)
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
  timeline: boolean;
  layout: BlockLayout;
  onChange: (layout: BlockLayout) => void;
}) {
  const available = layoutsFor(props.kind, props.timeline);
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
    case "medium":
      return "medium";
    case "large":
      return "normal";
    default:
      return "small";
  }
}

function SemanticThumbnail(props: {
  item: SemanticContentItem;
  layout: BlockLayout;
}) {
  const [failed, setFailed] = useState(false);
  if (props.item.documentUri === null || failed) {
    return <span className="semantic-item-placeholder" aria-hidden="true">◇</span>;
  }

  return (
    <img
      src={documentContentUrl(props.item.documentUri, previewVariant(props.layout))}
      alt=""
      loading="lazy"
      onError={() => setFailed(true)}
    />
  );
}

function ItemName(props: {
  item: SemanticContentItem;
  showSection: boolean;
}) {
  return (
    <div className="semantic-item-name">
      <a href={resourceHref(props.item.resourceId)} onClick={followAppLink}>
        {props.item.title}
      </a>
      {props.item.detail && <span>{props.item.detail}</span>}
      {props.showSection && <small>{props.item.sectionTitle}</small>}
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
      <div className="semantic-table-wrap">
        <table className="semantic-content-table">
          {!props.hideTableHeader && (
            <thead>
              <tr>
                <th className="semantic-media-column"><span className="visually-hidden">Изображение</span></th>
                <th>Название</th>
                <th>Дата</th>
              </tr>
            </thead>
          )}
          <tbody>
            {props.items.map(item => (
              <tr key={item.key}>
                <td className="semantic-media-column">
                  <a href={resourceHref(item.resourceId)} onClick={followAppLink} tabIndex={-1}>
                    <SemanticThumbnail item={item} layout="small" />
                  </a>
                </td>
                <td><ItemName item={item} showSection={props.showSection} /></td>
                <td className="semantic-date-cell">{item.displayDate ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
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
            >
              <SemanticThumbnail item={item} layout={props.layout} />
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
            >
              <SemanticThumbnail item={item} layout="small" />
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

function TimelineBlockBody(props: {
  items: SemanticContentItem[];
  layout: BlockLayout;
}) {
  const size = pageSize(props.layout);
  const [visibleCount, setVisibleCount] = useState(size);
  const loadMoreRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setVisibleCount(size);
  }, [props.items, size]);

  useEffect(() => {
    const target = loadMoreRef.current;
    if (target === null || visibleCount >= props.items.length) return;

    const observer = new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting)) {
        setVisibleCount(value => Math.min(props.items.length, value + size));
      }
    }, { rootMargin: "700px 0px" });

    observer.observe(target);
    return () => observer.disconnect();
  }, [props.items.length, size, visibleCount]);

  const visible = props.items.slice(0, visibleCount);
  const firstUndated = visible.findIndex(item => item.sortDate === null);
  const dated = firstUndated < 0 ? visible : visible.slice(0, firstUndated);
  const undated = firstUndated < 0 ? [] : visible.slice(firstUndated);
  const hasMore = visible.length < props.items.length;

  return (
    <>
      <BlockItems items={dated} layout={props.layout} showSection />
      {undated.length > 0 && (
        <>
          <div className="timeline-undated-label">Без указанной даты</div>
          <BlockItems
            items={undated}
            layout={props.layout}
            showSection
            hideTableHeader={dated.length > 0}
          />
        </>
      )}
      {hasMore && (
        <div className="timeline-load-more" ref={loadMoreRef}>
          <span>Показано {visible.length} из {props.items.length}</span>
          <button
            className="button ghost compact"
            type="button"
            onClick={() => setVisibleCount(value => Math.min(props.items.length, value + size))}
          >
            Показать ещё
          </button>
        </div>
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
          timeline={timeline}
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
  const allSelected = props.selected.size === props.blocks.length;
  return (
    <details className="semantic-sections-menu">
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

export function SemanticContentBlocks({ blocks }: SemanticContentBlocksProps) {
  const nonEmpty = useMemo(
    () => blocks.filter(block => block.items.length > 0),
    [blocks]
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
      sectionKey: key,
      sectionTitle: title,
      documentUri: photo.documentUri,
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
    kind: links.some(link => link.documentUri != null) ? "media" : "text",
    items: links.map(link => ({
      key: `${key}:${link.relationResourceId ?? link.resourceId}:${link.resourceId}:${link.relationLabel}`,
      resourceId: link.resourceId,
      title: link.displayName,
      detail: [link.relationLabel, link.typeLabel].filter(Boolean).join(" · ") || null,
      sectionKey: key,
      sectionTitle: title,
      documentUri: link.documentUri ?? null,
      displayDate: link.displayDate ?? null,
      sortDate: link.sortDate ?? null
    }))
  };
}
