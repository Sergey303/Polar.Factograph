import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { errorText } from "../api/errorText";
import type { DocumentLocation, DocumentVariant } from "../api/models";
import {
  documentBlobQueryOptions,
  documentLocationQueryOptions
} from "./queryOptions";

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
  const locationQuery = useQuery(documentLocationQueryOptions(uri, token));
  const location = locationQuery.data ?? null;
  const variant = location === null ? null : preferredVariant(location, policy);
  const blobQuery = useQuery({
    ...documentBlobQueryOptions(uri, variant ?? "original", token),
    enabled: variant !== null
  });
  const blob = variant === null ? null : blobQuery.data ?? null;
  const [objectUrl, setObjectUrl] = useState<string | null>(null);

  useEffect(() => {
    if (blob === null) {
      setObjectUrl(null);
      return;
    }

    const url = URL.createObjectURL(blob);
    setObjectUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [blob]);

  const errors = [locationQuery.error, blobQuery.error]
    .filter(error => error !== null)
    .map(errorText);

  return {
    location,
    objectUrl,
    contentType: blob?.type ?? "",
    loading: locationQuery.isPending || (variant !== null && blobQuery.isPending),
    error: errors.length > 0 ? [...new Set(errors)].join(" · ") : null,
    reload: () => {
      void locationQuery.refetch();
      if (variant !== null) void blobQuery.refetch();
    }
  };
}
