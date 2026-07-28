import { useCallback, useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { DocumentLocation, DocumentVariant } from "../api/models";

export type DocumentPreviewPolicy = "smallest" | "largest-preview";

function preferredVariant(
  location: DocumentLocation,
  policy: DocumentPreviewPolicy
): DocumentVariant | null {
  if (policy === "smallest") {
    if (location.smallPreviewAvailable) return "small";
    if (location.mediumPreviewAvailable) return "medium";
    if (location.normalPreviewAvailable) return "normal";
  } else {
    if (location.normalPreviewAvailable) return "normal";
    if (location.mediumPreviewAvailable) return "medium";
    if (location.smallPreviewAvailable) return "small";
  }

  return location.originalAvailable ? "original" : null;
}

export function useDocumentAsset(
  uri: string,
  token: string,
  policy: DocumentPreviewPolicy = "smallest"
) {
  const [location, setLocation] = useState<DocumentLocation | null>(null);
  const [objectUrl, setObjectUrl] = useState<string | null>(null);
  const [contentType, setContentType] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);
  const reload = useCallback(() => setRevision(value => value + 1), []);

  useEffect(() => {
    const controller = new AbortController();
    let createdUrl: string | null = null;
    setLoading(true);
    setError(null);
    setLocation(null);
    setObjectUrl(null);
    setContentType("");

    async function load(): Promise<void> {
      const nextLocation = await factographApi.getDocumentLocation(
        uri,
        token,
        controller.signal
      );
      setLocation(nextLocation);

      const variant = preferredVariant(nextLocation, policy);
      if (variant === null) {
        return;
      }

      const blob = await factographApi.getDocumentBlob(
        uri,
        variant,
        token,
        controller.signal
      );
      createdUrl = URL.createObjectURL(blob);
      setContentType(blob.type);
      setObjectUrl(createdUrl);
    }

    load()
      .catch(reason => {
        if (!controller.signal.aborted) setError(errorText(reason));
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => {
      controller.abort();
      if (createdUrl !== null) URL.revokeObjectURL(createdUrl);
    };
  }, [uri, token, policy, revision]);

  return { location, objectUrl, contentType, loading, error, reload };
}
