import type { CollectionItem } from "../api/collectionModels";

interface CollectionItemListProps {
  items: CollectionItem[];
  selectedId: string | null;
  canRemove: (item: CollectionItem) => boolean;
  busy: boolean;
  onSelect: (resourceId: string) => void;
  onOpenCollection: (resourceId: string) => void;
  onRemove: (item: CollectionItem) => void;
}

export function CollectionItemList({
  items,
  selectedId,
  canRemove,
  busy,
  onSelect,
  onOpenCollection,
  onRemove
}: CollectionItemListProps) {
  if (items.length === 0) {
    return <p className="muted">В коллекции нет доступных элементов.</p>;
  }

  return (
    <ul className="collection-item-list">
      {items.map(item => (
        <li
          key={item.membershipResourceId}
          className={item.resourceId === selectedId ? "selected" : undefined}
        >
          <button
            className="collection-item-main"
            onClick={() => onSelect(item.resourceId)}
          >
            <strong>{item.displayName}</strong>
            <span className="muted">{item.typeLabel ?? item.type ?? "Ресурс"}</span>
            <span className="muted mono">{item.resourceId}</span>
          </button>
          <div className="collection-item-actions">
            <button
              className="button subtle compact"
              onClick={() => onOpenCollection(item.resourceId)}
            >
              Открыть как коллекцию
            </button>
            {canRemove(item) && (
              <button
                className="button danger compact"
                onClick={() => onRemove(item)}
                disabled={busy}
              >
                Удалить связь
              </button>
            )}
          </div>
        </li>
      ))}
    </ul>
  );
}
