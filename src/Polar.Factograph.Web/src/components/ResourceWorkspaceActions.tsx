interface ResourceWorkspaceActionsProps {
  canCreate: boolean;
  canEdit: boolean;
  notice: string | null;
  onCreate: () => void;
  onEdit: () => void;
}

export function ResourceWorkspaceActions(props: ResourceWorkspaceActionsProps) {
  if (!props.canCreate && !props.canEdit && props.notice === null) return null;

  return (
    <div className="resource-workspace-actions">
      <div className="button-row">
        {props.canCreate && (
          <button className="button primary" type="button" onClick={props.onCreate}>
            Создать ресурс
          </button>
        )}
        {props.canEdit && (
          <button className="button subtle" type="button" onClick={props.onEdit}>
            Редактировать
          </button>
        )}
      </div>
      {props.notice && <div className="notice">{props.notice}</div>}
    </div>
  );
}