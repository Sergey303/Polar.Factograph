import { useEffect, useState } from "react";

export type AppRoute =
  | { page: "search" }
  | { page: "resource"; resourceId: string };

function applicationBase(): URL {
  const explicitBase = document
    .querySelector<HTMLBaseElement>("base[href]")
    ?.getAttribute("href")
    ?.trim();

  if (!explicitBase) {
    return new URL("/", window.location.origin);
  }

  try {
    const candidate = new URL(explicitBase, `${window.location.origin}/`);
    if (candidate.origin !== window.location.origin) {
      return new URL("/", window.location.origin);
    }

    candidate.search = "";
    candidate.hash = "";
    if (!candidate.pathname.endsWith("/")) {
      candidate.pathname += "/";
    }
    return candidate;
  } catch {
    return new URL("/", window.location.origin);
  }
}

function applicationHref(hash: string): string {
  const target = applicationBase();
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
