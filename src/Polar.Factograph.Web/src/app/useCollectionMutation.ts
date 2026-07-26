import { useState } from "react";
import { collectionApi } from "../api/collectionApi";
import type { CollectionItem } from "../api/collectionModels";
import { errorText } from "../api/errorText";

export function useCollectionMutation(
  collectionId: string | null,
  cassetteId: string | null,
  token: string,
  onChanged: () => void
) {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function add(resourceId: string): Promise<void> {
    if (collectionId === null || resourceId.trim().length === 0) {
      return;
    }
    await run(async () => {
      const result = await collectionApi.addItem(
        collectionId,
        resourceId.trim(),
        cassetteId,
        token
      );
      setMessage(result.indexReady
        ? "Элемент добавлен."
        : "Элемент записан, индекс требует восстановления.");
    });
  }

  async function remove(item: CollectionItem): Promise<void> {
    if (collectionId === null) {
      return;
    }
    await run(async () => {
      const result = await collectionApi.removeItem(
        item.membershipResourceId,
        collectionId,
        item.resourceId,
        item.membershipCassetteId,
        token
      );
      setMessage(result.indexReady
        ? "Элемент удалён."
        : "Удаление записано, индекс требует восстановления.");
    });
  }

  async function run(action: () => Promise<void>): Promise<void> {
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      await action();
      onChanged();
    } catch (reason) {
      setError(errorText(reason));
    } finally {
      setBusy(false);
    }
  }

  return { busy, message, error, add, remove };
}
