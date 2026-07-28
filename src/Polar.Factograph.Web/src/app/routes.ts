import { useEffect, useState } from "react";

export type AppRoute =
  | { page: "search" }
  | { page: "resource"; resourceId: string };

const routeChangedEvent = "factograph:route-changed";
const applicationBaseMeta = "meta[name='factograph-app-base']";

function rootBase(): URL {
  return new URL("/", window.location.origin);
}

function isApiPath(pathname: string): boolean {
  const normalized = pathname.toLowerCase();
  return normalized === "/api" || normalized.startsWith("/api/");
}

function applicationBase(): URL {
  const configuredBase = document
    .querySelector<HTMLMetaElement>(applicationBaseMeta)
    ?.content
    .trim();

  if (!configuredBase) {
    return rootBase();
  }

  try {
    const candidate = new URL(configuredBase, rootBase());
    if (candidate.origin !== window.location.origin || isApiPath(candidate.pathname)) {
      return rootBase();
    }

    candidate.search = "";
    candidate.hash = "";
    if (!candidate.pathname.endsWith("/")) {
      candidate.pathname += "/";
    }
    return candidate;
  } catch {
    return rootBase();
  }
}

function applicationHref(hash: string): string {
  const target = applicationBase();
  target.hash = hash;
  return target.href;
}

function normalizeApplicationLocation(): void {
  const target = applicationBase();
  const currentPath = window.location.pathname.endsWith("/")
    ? window.location.pathname
    : `${window.location.pathname}/`;

  if (currentPath === target.pathname && window.location.search.length === 0) {
    return;
  }

  target.hash = window.location.hash || "/search";
  window.history.replaceState(null, "", target.href);
}

function navigate(hash: string, replace = false): void {
  const target = applicationHref(hash);
  if (replace) {
    window.history.replaceState(null, "", target);
  } else {
    window.history.pushState(null, "", target);
  }
  window.dispatchEvent(new Event(routeChangedEvent));
}

export const searchHref = applicationHref("/search");

export function resourceHref(resourceId: string): string {
  return applicationHref(`/resource/${encodeURIComponent(resourceId)}`);
}

export function navigateToSearch(replace = false): void {
  navigate("/search", replace);
}

export function navigateToResource(resourceId: string, replace = false): void {
  navigate(`/resource/${encodeURIComponent(resourceId)}`, replace);
}

function currentRoute(): AppRoute {
  normalizeApplicationLocation();

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
    window.addEventListener("popstate", changed);
    window.addEventListener(routeChangedEvent, changed);
    return () => {
      window.removeEventListener("hashchange", changed);
      window.removeEventListener("popstate", changed);
      window.removeEventListener(routeChangedEvent, changed);
    };
  }, []);

  return route;
}
