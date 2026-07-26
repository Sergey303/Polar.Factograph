interface CollectionOpenFormProps {
  value: string;
  loading: boolean;
  canGoBack: boolean;
  onChange: (value: string) => void;
  onOpen: () => void;
  onBack: () => void;
  onClear: () => void;
}

export function CollectionOpenForm({
  value,
  loading,
  canGoBack,
  onChange,
  onOpen,
  onBack,
  onClear
}: CollectionOpenFormProps) {
  return (
    <form
      className="collection-open-form"
      onSubmit={event => {
        event.preventDefault();
        onOpen();
      }}
    >
      <label>
        <span>Идентификатор коллекции</span>
        <input
          value={value}
          onChange={event => onChange(event.target.value)}
          placeholder="collection-1"
        />
      </label>
      <div className="button-row">
        <button className="button primary compact" disabled={loading}>Открыть</button>
        <button type="button" className="button subtle compact" onClick={onBack} disabled={!canGoBack}>Назад</button>
        <button type="button" className="button subtle compact" onClick={onClear}>Сбросить</button>
      </div>
    </form>
  );
}
