import { useState } from "react";
import { errorText } from "../api/errorText";
import { resourceWriteApi } from "../api/resourceWriteApi";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import {
  toResourceWriteRequest,
  validateResourceDraft
} from "./resourceDraftFactory";
import type { ResourceDraft } from "./resourceDraftModels";

export function useResourceWrite(
  token: string,
  onSaved: (result: ResourceWriteResponse) => void
) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function save(draft: ResourceDraft): Promise<void> {
    const validation = validateResourceDraft(draft);
    if (validation !== null) {
      setError(validation);
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const result = await resourceWriteApi.write(
        toResourceWriteRequest(draft),
        token
      );
      onSaved(result);
    } catch (reason) {
      setError(errorText(reason));
    } finally {
      setBusy(false);
    }
  }

  return { busy, error, save };
}