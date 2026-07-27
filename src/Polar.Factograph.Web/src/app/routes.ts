import { useEffect, useState } from "react";

export type AppRoute =
  | { page: "search" }
  | { page: "resource"; resourceId: string };

function applicationHref(hash: string): string {
  const target = new URL(document.baseURI);
  target.hash = hash;
  return target.href;
}

export const searchHref = applicationHref("/search");

export function resourceHref(resourceId: string): string {
  return applicationHref(`/resource/${encodeURIComponent(resourceId)}`);
}

function currentRoute(): AppRoute {
  const prefix = "#/resource/";
  if (window.location.hash.startsWith(prefix)) {
    const encoded = window.location.hash.slice(prefix.length);
    try {
      const resourceId = decodeURIComponent(encoded);
      if (resourceId.trim().length > 0) {
        return { page: "resource", resourceId };
      }
    } catch {
      return { page: "search" };
    }
  }

  return { page: "search" };
}

export function useAppRoute(): AppRoute {
  const [route, setRoute] = useState<AppRoute>(currentRoute);

  useEffect(() => {
    const changed = () => setRoute(currentRoute());
    window.addEventListener("hashchange", changed);
    return () => window.removeEventListener("hashchange", changed);
  }, []);

  return route;
}
