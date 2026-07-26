import { useCallback, useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { DocumentLocation, DocumentVariant } from "../api/models";

function preferredVariant(location: DocumentLocation): DocumentVariant {
  if (location.normalPreviewAvailable) return "normal";
  if (location.mediumPreviewAvailable) return "medium";
  if (location.smallPreviewAvailable) return "small";
  return "original";
}

export function useDocumentAsset(uri: string, token: string) {
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

    async function load(): Promise<void> {
      const nextLocation = await factographApi.getDocumentLocation(
        uri,
        token,
        controller.signal
      );
      const blob = await factographApi.getDocumentBlob(
        uri,
        preferredVariant(nextLocation),
        token,
        controller.signal
      );
      createdUrl = URL.createObjectURL(blob);
      setLocation(nextLocation);
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
  }, [uri, token, revision]);

  return { location, objectUrl, contentType, loading, error, reload };
}
