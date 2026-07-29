import { useEffect, useState } from "react";

export type ResourceRouteMode = "view" | "edit" | "relations" | "document";

export type AppRoute =
  | { page: "search"; query: string }
  | { page: "create-entity" }
  | { page: "resource"; resourceId: string; mode: ResourceRouteMode };

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

function searchHash(query: string): string {
  const normalized = query.trim();
  if (normalized.length === 0) return "/search";
  const parameters = new URLSearchParams({ q: normalized });
  return `/search?${parameters.toString()}`;
}

function resourceHash(resourceId: string, mode: ResourceRouteMode): string {
  const base = `/resource/${encodeURIComponent(resourceId)}`;
  switch (mode) {
    case "edit":
      return `${base}/edit`;
    case "relations":
      return `${base}/relations`;
    case "document":
      return `${base}/documents/new`;
    default:
      return base;
  }
}

export const searchHref = applicationHref("/search");
export const createEntityHref = applicationHref("/entity/new");

export function searchHrefFor(query: string): string {
  return applicationHref(searchHash(query));
}

export function resourceHref(
  resourceId: string,
  mode: ResourceRouteMode = "view"
): string {
  return applicationHref(resourceHash(resourceId, mode));
}

export function navigateToSearch(query = "", replace = false): void {
  navigate(searchHash(query), replace);
}

export function navigateToCreateEntity(replace = false): void {
  navigate("/entity/new", replace);
}

export function navigateToResource(resourceId: string, replace = false): void {
  navigate(resourceHash(resourceId, "view"), replace);
}

export function navigateToResourceMode(
  resourceId: string,
  mode: ResourceRouteMode,
  replace = false
): void {
  navigate(resourceHash(resourceId, mode), replace);
}

function currentHash(): { path: string; parameters: URLSearchParams } {
  const raw = window.location.hash.startsWith("#")
    ? window.location.hash.slice(1)
    : window.location.hash;
  const separator = raw.indexOf("?");
  if (separator < 0) {
    return { path: raw || "/search", parameters: new URLSearchParams() };
  }
  return {
    path: raw.slice(0, separator) || "/search",
    parameters: new URLSearchParams(raw.slice(separator + 1))
  };
}

function parseResourceMode(suffix: string): ResourceRouteMode | null {
  switch (suffix) {
    case "":
      return "view";
    case "/edit":
      return "edit";
    case "/relations":
      return "relations";
    case "/documents/new":
      return "document";
    default:
      return null;
  }
}

function currentRoute(): AppRoute {
  normalizeApplicationLocation();
  const hash = currentHash();

  if (hash.path === "/search") {
    return { page: "search", query: hash.parameters.get("q")?.trim() ?? "" };
  }

  if (hash.path === "/entity/new") {
    return { page: "create-entity" };
  }

  const prefix = "/resource/";
  if (hash.path.startsWith(prefix)) {
    const remainder = hash.path.slice(prefix.length);
    const slash = remainder.indexOf("/");
    const encoded = slash < 0 ? remainder : remainder.slice(0, slash);
    const suffix = slash < 0 ? "" : remainder.slice(slash);
    const mode = parseResourceMode(suffix);
    if (mode !== null) {
      try {
        const resourceId = decodeURIComponent(encoded);
        if (resourceId.trim().length > 0) {
          return { page: "resource", resourceId, mode };
        }
      } catch {
        return { page: "search", query: "" };
      }
    }
  }

  return { page: "search", query: "" };
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
