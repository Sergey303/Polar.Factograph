import { useEffect, useMemo, useState } from "react";
import { documentContentUrl } from "../../api/factographApi";
import { followAppLink, resourceHref } from "../../app/routes";
import "../../styles/simple-semantic-content.css";
import type {
  SemanticContentBlockDefinition,
  SemanticContentItem,
  SemanticContentMember
} from "./model";

interface SemanticContentBlocksProps {
  blocks: SemanticContentBlockDefinition[];
  currentResourceId?: string;
}

type PhotoSize = "small" | "medium" | "large";

interface TextRow {
  key: string;
  blockKey: string;
  blockTitle: string;
  roleLabel: string | null;
  resourceId: string;
  displayName: string;
  values: string[];
}

const photoSizeLabels: Record<PhotoSize, string> = {
  small: "Маленькие",
  medium: "Средние",
  large: "Большие"
};

function storedPhotoSize(): PhotoSize {
  try {
    const value = window.localStorage.getItem("polar-factograph:public-photo-size");
    return value === "medium" || value === "large" ? value : "small";
  } catch {
    return "small";
  }
}

function textPageSize(): number {
  const available = typeof window === "undefined" ? 720 : window.innerHeight - 300;
  return Math.max(8, Math.min(24, Math.floor(available / 38)));
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
        if (item.resourceId !== currentResourceId) items.push(item);
        continue;
      }

      const members = item.members.filter(member => member.resourceId !== currentResourceId);
      if (members.length === 0 && !item.hasDocument) continue;

      let next: SemanticContentItem = { ...item, members };
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

function publicMembers(item: SemanticContentItem): SemanticContentMember[] {
  return item.members?.filter(member => !member.hasDocument) ?? [];
}

function textRows(blocks: SemanticContentBlockDefinition[]): TextRow[] {
  const rows: TextRow[] = [];
  for (const block of blocks) {
    for (const item of block.items) {
      if (item.hasDocument) continue;
      const members = publicMembers(item);
      if (members.length === 0) {
        rows.push({
          key: `${block.key}:${item.key}`,
          blockKey: block.key,
          blockTitle: block.title,
          roleLabel: null,
          resourceId: item.resourceId,
          displayName: item.title,
          values: item.values
        });
        continue;
      }

      members.forEach((member, index) => rows.push({
        key: `${block.key}:${item.key}:${member.resourceId}:${index}`,
        blockKey: block.key,
        blockTitle: block.title,
        roleLabel: member.roleLabel,
        resourceId: member.resourceId,
        displayName: member.displayName,
        values: index === 0 ? item.values : []
      }));
    }
  }
  return rows;
}

function PhotoCard({ item, size }: { item: SemanticContentItem; size: PhotoSize }) {
  const source = item.documentUri === null
    ? null
    : documentContentUrl(item.documentUri, size === "large" ? "normal" : size);
  const related = publicMembers(item);

  return (
    <article className="simple-photo-card">
      <a
        className="simple-photo-link"
        href={resourceHref(item.resourceId)}
        onClick={followAppLink}
        aria-label="Открыть фотографию"
      >
        {source === null ? (
          <span className="simple-photo-placeholder" aria-hidden="true">▧</span>
        ) : (
          <img src={source} alt="" loading="lazy" />
        )}
      </a>
      {(item.displayDate || item.values.length > 0 || related.length > 0) && (
        <div className="simple-photo-caption">
          {item.displayDate && <time>{item.displayDate}</time>}
          {item.values.map((value, index) => (
            <span key={`${value}:${index}`}>{value}</span>
          ))}
          {related.map((member, index) => (
            <span key={`${member.resourceId}:${index}`}>
              {member.roleLabel && <small>{member.roleLabel}</small>}
              <a href={resourceHref(member.resourceId)} onClick={followAppLink}>
                {member.displayName}
              </a>
            </span>
          ))}
        </div>
      )}
    </article>
  );
}

function PhotoColumn({ items }: { items: SemanticContentItem[] }) {
  const [size, setSize] = useState<PhotoSize>(storedPhotoSize);

  function changeSize(next: PhotoSize): void {
    setSize(next);
    try {
      window.localStorage.setItem("polar-factograph:public-photo-size", next);
    } catch {
      // The selected size still applies for the current page.
    }
  }

  return (
    <section className="simple-archive-photos">
      <header className="simple-column-header">
        <h2>Фотографии</h2>
        <div className="simple-photo-size" role="group" aria-label="Размер фотографий">
          {(Object.keys(photoSizeLabels) as PhotoSize[]).map(option => (
            <button
              key={option}
              type="button"
              className={option === size ? "selected" : ""}
              aria-pressed={option === size}
              onClick={() => changeSize(option)}
            >
              {photoSizeLabels[option]}
            </button>
          ))}
        </div>
      </header>
      <div className={`simple-photo-grid simple-photo-grid-${size}`}>
        {items.map(item => <PhotoCard key={item.key} item={item} size={size} />)}
      </div>
    </section>
  );
}

function TextColumn({ rows }: { rows: TextRow[] }) {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(textPageSize);
  const fingerprint = rows.map(row => row.key).join("\n");

  useEffect(() => {
    const resized = () => setPageSize(textPageSize());
    window.addEventListener("resize", resized);
    return () => window.removeEventListener("resize", resized);
  }, []);

  useEffect(() => {
    setPage(0);
  }, [fingerprint, pageSize]);

  const pageCount = Math.max(1, Math.ceil(rows.length / pageSize));
  const currentPage = Math.min(page, pageCount - 1);
  const visible = rows.slice(currentPage * pageSize, (currentPage + 1) * pageSize);
  const grouped = useMemo(() => {
    const groups: Array<{ key: string; title: string; rows: TextRow[] }> = [];
    for (const row of visible) {
      const last = groups[groups.length - 1];
      if (last?.key === row.blockKey) last.rows.push(row);
      else groups.push({ key: row.blockKey, title: row.blockTitle, rows: [row] });
    }
    return groups;
  }, [visible]);

  return (
    <aside className="simple-archive-links">
      <div className="simple-link-groups">
        {grouped.map(group => (
          <section className="simple-link-group" key={`${group.key}:${currentPage}`}>
            <h2>{group.title}</h2>
            <ul>
              {group.rows.map(row => (
                <li key={row.key}>
                  {row.roleLabel && <span className="simple-link-role">{row.roleLabel}</span>}
                  <a href={resourceHref(row.resourceId)} onClick={followAppLink}>
                    {row.displayName}
                  </a>
                  {row.values.map((value, index) => (
                    <span className="simple-link-value" key={`${value}:${index}`}>{value}</span>
                  ))}
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>
      {pageCount > 1 && (
        <nav className="simple-link-pagination" aria-label="Страницы связей">
          <button
            type="button"
            disabled={currentPage === 0}
            onClick={() => setPage(value => Math.max(0, value - 1))}
          >
            Предыдущее
          </button>
          <span>{currentPage + 1} из {pageCount}</span>
          <button
            type="button"
            disabled={currentPage >= pageCount - 1}
            onClick={() => setPage(value => Math.min(pageCount - 1, value + 1))}
          >
            Следующее
          </button>
        </nav>
      )}
    </aside>
  );
}

export function SemanticContentBlocks({
  blocks,
  currentResourceId
}: SemanticContentBlocksProps) {
  const prepared = useMemo(
    () => prepareBlocks(blocks, currentResourceId),
    [blocks, currentResourceId]
  );
  const photos = prepared.flatMap(block => block.items.filter(item => item.hasDocument));
  const rows = useMemo(() => textRows(prepared), [prepared]);

  if (photos.length === 0 && rows.length === 0) return null;

  return (
    <div className={`simple-archive-layout ${photos.length === 0 ? "without-photos" : ""}`}>
      {photos.length > 0 && <PhotoColumn items={photos} />}
      {rows.length > 0 && <TextColumn rows={rows} />}
    </div>
  );
}
