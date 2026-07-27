import type { OntologyWriteClass } from "../api/ontologyModels";
import type { ProjectCassetteOverview } from "../api/models";
import type { ResourceDraft } from "../app/resourceDraftModels";

interface ResourceEditorHeaderProps {
  mode: "create" | "edit";
  draft: ResourceDraft;
  classes: OntologyWriteClass[];
  cassettes: ProjectCassetteOverview[];
  lockType?: boolean;
  lockCassette?: boolean;
  onTypeChange: (value: string) => void;
  onFieldChange: (field: "resourceId" | "cassetteId", value: string) => void;
}

export function ResourceEditorHeader(props: ResourceEditorHeaderProps) {
  return (
    <div className="resource-editor-header-fields">
      <label>
        <span>Тип ресурса</span>
        <select
          value={props.draft.typeId}
          onChange={event => props.onTypeChange(event.target.value)}
          disabled={props.mode === "edit" || props.lockType}
        >
          <option value="">Выберите тип</option>
          {props.classes.map(type => (
            <option key={type.id} value={type.id}>{type.label}</option>
          ))}
        </select>
      </label>

      <label>
        <span>Идентификатор</span>
        <input
          value={props.draft.resourceId}
          onChange={event => props.onFieldChange("resourceId", event.target.value)}
          placeholder="Оставьте пустым для автоматического ID"
          readOnly={props.mode === "edit"}
        />
      </label>

      <label>
        <span>Кассета записи</span>
        <select
          value={props.draft.cassetteId}
          onChange={event => props.onFieldChange("cassetteId", event.target.value)}
          disabled={props.lockCassette}
        >
          <option value="">Выберите кассету</option>
          {props.cassettes.map(cassette => (
            <option key={cassette.id} value={cassette.id}>{cassette.name}</option>
          ))}
        </select>
      </label>
    </div>
  );
}
