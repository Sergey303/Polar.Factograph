interface ResourceWorkspaceActionsProps {
  canCreate: boolean;
  canAddDocument: boolean;
  canAddRelation: boolean;
  canEdit: boolean;
  notice: string | null;
  onCreate: () => void;
  onAddDocument: () => void;
  onAddRelation: () => void;
  onEdit: () => void;
}

export function ResourceWorkspaceActions(props: ResourceWorkspaceActionsProps) {
  const hasActions = props.canCreate ||
    props.canAddDocument ||
    props.canAddRelation ||
    props.canEdit;
  if (!hasActions && props.notice === null) return null;

  return (
    <div className="resource-workspace-actions">
      <div className="button-row">
        {props.canCreate && (
          <button className="button primary" type="button" onClick={props.onCreate}>
            Создать сущность
          </button>
        )}
        {props.canAddDocument && (
          <button className="button subtle" type="button" onClick={props.onAddDocument}>
            Добавить документ
          </button>
        )}
        {props.canAddRelation && (
          <button className="button subtle" type="button" onClick={props.onAddRelation}>
            Связи
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
