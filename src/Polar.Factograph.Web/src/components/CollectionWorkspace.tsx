import type { ProjectOverview } from "../api/models";
import { hasCassetteRight, hasDefaultCassetteRight } from "../app/projectAccess";
import { useCollectionContents } from "../app/useCollectionContents";
import { useCollectionMutation } from "../app/useCollectionMutation";
import { useCollectionNavigation } from "../app/useCollectionNavigation";
import { CollectionItemList } from "./CollectionItemList";
import { CollectionMutationPanel } from "./CollectionMutationPanel";
import { CollectionOpenForm } from "./CollectionOpenForm";

interface CollectionWorkspaceProps {
  project: ProjectOverview | null;
  token: string;
  selectedResourceId: string | null;
  onSelect: (resourceId: string) => void;
}

export function CollectionWorkspace({
  project,
  token,
  selectedResourceId,
  onSelect
}: CollectionWorkspaceProps) {
  const navigation = useCollectionNavigation();
  const collection = useCollectionContents(navigation.currentId, token);
  const mutation = useCollectionMutation(
    navigation.currentId,
    project?.defaultWriteCassetteId ?? null,
    token,
    collection.reload
  );
  const canAdd = hasDefaultCassetteRight(project, "writeMetadata");

  return (
    <section className="navigation-section collection-workspace">
      <div className="panel-heading compact-heading">
        <span className="eyebrow">Навигация</span>
        <h2>Коллекция</h2>
      </div>
      <CollectionOpenForm
        value={navigation.input}
        loading={collection.loading}
        canGoBack={navigation.canGoBack}
        onChange={navigation.setInput}
        onOpen={() => navigation.open()}
        onBack={navigation.back}
        onClear={navigation.clear}
      />

      {navigation.currentId && (
        <span className="muted mono current-collection-id">{navigation.currentId}</span>
      )}
      {collection.loading && <p className="muted">Загрузка коллекции…</p>}
      {collection.error && <div className="notice error">{collection.error}</div>}
      {collection.contents && (
        <>
          <CollectionMutationPanel
            selectedResourceId={selectedResourceId}
            canAdd={canAdd}
            busy={mutation.busy}
            message={mutation.message}
            error={mutation.error}
            onAdd={resourceId => void mutation.add(resourceId)}
          />
          <CollectionItemList
            items={collection.contents.items}
            selectedId={selectedResourceId}
            canRemove={item => hasCassetteRight(
              project,
              item.membershipCassetteId,
              "delete"
            )}
            busy={mutation.busy}
            onSelect={onSelect}
            onOpenCollection={navigation.open}
            onRemove={item => {
              if (window.confirm(`Удалить ${item.displayName} из коллекции?`)) {
                void mutation.remove(item);
              }
            }}
          />
        </>
      )}
    </section>
  );
}
