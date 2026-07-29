import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import type { OntologyWriteSchema } from "../api/ontologyModels";
import { errorText } from "../api/errorText";
import { resourceWriteApi } from "../api/resourceWriteApi";
import type { ResourceWriteResponse } from "../api/resourceWriteModels";
import { queryKeys } from "./queryOptions";
import { toResourceWriteRequest } from "./resourceDraftFactory";
import type { ResourceDraft } from "./resourceDraftModels";
import { validateResourceDraft } from "./resourceDraftValidation";

export function useResourceWrite(
  token: string,
  schema: OntologyWriteSchema,
  onSaved: (result: ResourceWriteResponse) => void
) {
  const queryClient = useQueryClient();
  const [validationError, setValidationError] = useState<string | null>(null);
  const mutation = useMutation({
    mutationFn: (draft: ResourceDraft) => resourceWriteApi.write(
      toResourceWriteRequest(draft),
      token
    ),
    onSuccess: (result, draft) => {
      const affectedResourceIds = new Set<string>([result.resourceId]);
      for (const property of draft.properties) {
        if (property.kind === "resource" && property.value.trim().length > 0) {
          affectedResourceIds.add(property.value.trim());
        }
      }

      for (const resourceId of affectedResourceIds) {
        void queryClient.invalidateQueries({
          queryKey: queryKeys.resourcePage(token, resourceId)
        });
        void queryClient.invalidateQueries({
          queryKey: queryKeys.portrait(token, resourceId)
        });
      }
      void queryClient.invalidateQueries({ queryKey: queryKeys.search(token) });
      void queryClient.invalidateQueries({
        queryKey: queryKeys.collectionContents(token)
      });
      onSaved(result);
    }
  });

  async function save(draft: ResourceDraft): Promise<void> {
    const error = validateResourceDraft(draft, schema);
    if (error !== null) {
      setValidationError(error);
      return;
    }

    setValidationError(null);
    mutation.reset();
    try {
      await mutation.mutateAsync(draft);
    } catch {
      // The mutation object exposes the request error to the form.
    }
  }

  return {
    busy: mutation.isPending,
    error: validationError ?? (mutation.error === null ? null : errorText(mutation.error)),
    save
  };
}
