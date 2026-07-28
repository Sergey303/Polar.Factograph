import { useCallback, useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { DocumentLocation, DocumentVariant } from "../api/models";

function preferredVariant(
  location: DocumentLocation,
  previewOnly: boolean
): DocumentVariant | null {
  if (location.normalPreviewAvailable) return "normal";
  if (location.mediumPreviewAvailable) return "medium";
  if (location.smallPreviewAvailable) return "small";
  return previewOnly ? null : "original";
}

async function imageWidth(blob: Blob): Promise<number | null> {
  const objectUrl = URL.createObjectURL(blob);
  try {
    const image = new Image();
    image.decoding = "async";
    image.src = objectUrl;
    await image.decode();
    return image.naturalWidth;
  } catch {
    return null;
  } finally {
    URL.revokeObjectURL(objectUrl);
  }
}

async function sharperImageWhenNeeded(
  blob: Blob,
  variant: DocumentVariant,
  location: DocumentLocation,
  uri: string,
  token: string,
  minimumPreviewImageWidth: number,
  signal: AbortSignal
): Promise<Blob> {
  if (
    minimumPreviewImageWidth <= 0 ||
    variant === "original" ||
    !location.originalAvailable ||
    !blob.type.startsWith("image/")
  ) {
    return blob;
  }

  const width = await imageWidth(blob);
  if (width === null || width >= minimumPreviewImageWidth) {
    return blob;
  }

  try {
    const original = await factographApi.getDocumentBlob(
      uri,
      "original",
      token,
      signal
    );
    return original.type.startsWith("image/") ? original : blob;
  } catch (reason) {
    if (signal.aborted) throw reason;
    return blob;
  }
}

export function useDocumentAsset(
  uri: string,
  token: string,
  previewOnly = false,
  minimumPreviewImageWidth = 0
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

      const variant = preferredVariant(nextLocation, previewOnly);
      if (variant === null) {
        return;
      }

      const preview = await factographApi.getDocumentBlob(
        uri,
        variant,
        token,
        controller.signal
      );
      const blob = await sharperImageWhenNeeded(
        preview,
        variant,
        nextLocation,
        uri,
        token,
        minimumPreviewImageWidth,
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
  }, [uri, token, previewOnly, minimumPreviewImageWidth, revision]);

  return { location, objectUrl, contentType, loading, error, reload };
}
