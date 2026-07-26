interface ResourceEditorLoadStateProps {
  loading: boolean;
  error: string | null;
  onCancel: () => void;
}

export function ResourceEditorLoadState(props: ResourceEditorLoadStateProps) {
  return (
    <div className="resource-editor load-state">
      {props.loading && <div className="empty-state"><strong>Загрузка схемы…</strong></div>}
      {props.error && <div className="notice error">{props.error}</div>}
      {!props.loading && props.error === null && (
        <div className="notice error">Схема записи недоступна.</div>
      )}
      <button className="button subtle" type="button" onClick={props.onCancel}>
        Вернуться к карточке
      </button>
    </div>
  );
}