interface CollectionMutationPanelProps {
  selectedResourceId: string | null;
  canAdd: boolean;
  busy: boolean;
  message: string | null;
  error: string | null;
  onAdd: (resourceId: string) => void;
}

export function CollectionMutationPanel({
  selectedResourceId,
  canAdd,
  busy,
  message,
  error,
  onAdd
}: CollectionMutationPanelProps) {
  if (!canAdd) {
    return <p className="muted">Добавление недоступно для основной кассеты записи.</p>;
  }

  return (
    <div className="collection-mutation-panel">
      <button
        className="button primary compact"
        disabled={busy || selectedResourceId === null}
        onClick={() => selectedResourceId && onAdd(selectedResourceId)}
      >
        Добавить выбранный ресурс
      </button>
      {selectedResourceId === null && (
        <span className="muted">Сначала выберите ресурс в результатах или карточке.</span>
      )}
      {message && <span className="notice success">{message}</span>}
      {error && <span className="notice error">{error}</span>}
    </div>
  );
}
