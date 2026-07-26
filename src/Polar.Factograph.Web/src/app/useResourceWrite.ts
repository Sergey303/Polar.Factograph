import { useState } from "react";
import type { OntologyWriteSchema } from "../api/ontologyModels";
import { errorText } from "../api/errorText";
import { resourceWriteApi } from "../api/resourceWriteApi";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { toResourceWriteRequest } from "./resourceDraftFactory";
import type { ResourceDraft } from "./resourceDraftModels";
import { validateResourceDraft } from "./resourceDraftValidation";

export function useResourceWrite(
  token: string,
  schema: OntologyWriteSchema,
  onSaved: (result: ResourceWriteResponse) => void
) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function save(draft: ResourceDraft): Promise<void> {
    const validation = validateResourceDraft(draft, schema);
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