import { type MouseEvent, useEffect, useState } from "react";

export type ResourceRouteMode = "view" | "edit" | "relations" | "document";

export type AppRoute =
  | { page: "search"; query: string; typeId: string | null }
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

function applicationHref(route: string): string {
  return new URL(route.replace(/^\/+/, ""), applicationBase()).href;
}

function navigate(route: string, replace = false): void {
  const target = applicationHref(route);
  if (replace) {
    window.history.replaceState(null, "", target);
  } else {
    window.history.pushState(null, "", target);
  }
  window.dispatchEvent(new Event(routeChangedEvent));
}

function searchRoute(query: string, typeId: string | null = null): string {
  const parameters = new URLSearchParams();
  const normalizedQuery = query.trim();
  const normalizedType = typeId?.trim() ?? "";
  if (normalizedQuery.length > 0) parameters.set("q", normalizedQuery);
  if (normalizedType.length > 0) parameters.set("type", normalizedType);
  const encoded = parameters.toString();
  return encoded.length === 0 ? "/search" : `/search?${encoded}`;
}

function resourceRoute(resourceId: string, mode: ResourceRouteMode): string {
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

export function searchHrefFor(query: string, typeId: string | null = null): string {
  return applicationHref(searchRoute(query, typeId));
}

export function resourceHref(
  resourceId: string,
  mode: ResourceRouteMode = "view"
): string {
  return applicationHref(resourceRoute(resourceId, mode));
}

export function canonicalResourceHref(resourceId: string): string {
  return resourceHref(resourceId, "view");
}

export function followAppLink(event: MouseEvent<HTMLAnchorElement>): void {
  if (
    event.button !== 0 ||
    event.metaKey ||
    event.ctrlKey ||
    event.shiftKey ||
    event.altKey ||
    event.currentTarget.target === "_blank"
  ) {
    return;
  }

  const target = new URL(event.currentTarget.href, window.location.href);
  const base = applicationBase();
  if (
    target.origin !== base.origin ||
    !target.pathname.startsWith(base.pathname) ||
    isApiPath(target.pathname)
  ) {
    return;
  }

  event.preventDefault();
  window.history.pushState(null, "", target.href);
  window.dispatchEvent(new Event(routeChangedEvent));
}

export function navigateToSearch(query = "", replace = false): void {
  navigate(searchRoute(query), replace);
}

export function navigateToSearchFilter(
  query: string,
  typeId: string | null,
  replace = false
): void {
  navigate(searchRoute(query, typeId), replace);
}

export function navigateToCreateEntity(replace = false): void {
  navigate("/entity/new", replace);
}

export function navigateToResource(resourceId: string, replace = false): void {
  navigate(resourceRoute(resourceId, "view"), replace);
}

export function navigateToResourceMode(
  resourceId: string,
  mode: ResourceRouteMode,
  replace = false
): void {
  navigate(resourceRoute(resourceId, mode), replace);
}

function migrateLegacyHashRoute(): void {
  const raw = window.location.hash.startsWith("#")
    ? window.location.hash.slice(1)
    : window.location.hash;
  if (!raw.startsWith("/")) return;

  window.history.replaceState(null, "", applicationHref(raw));
}

function applicationPath(): string | null {
  const basePath = applicationBase().pathname;
  const currentPath = window.location.pathname;
  if (!currentPath.startsWith(basePath)) return null;

  const relative = currentPath.slice(basePath.length).replace(/^\/+/, "");
  return relative.length === 0 ? "/" : `/${relative}`;
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
  migrateLegacyHashRoute();
  const path = applicationPath();
  if (path === null || path === "/") {
    window.history.replaceState(null, "", applicationHref("/search"));
    return { page: "search", query: "", typeId: null };
  }

  if (path === "/search") {
    const parameters = new URLSearchParams(window.location.search);
    return {
      page: "search",
      query: parameters.get("q")?.trim() ?? "",
      typeId: parameters.get("type")?.trim() || null
    };
  }

  if (path === "/entity/new") {
    return { page: "create-entity" };
  }

  const prefix = "/resource/";
  if (path.startsWith(prefix)) {
    const remainder = path.slice(prefix.length);
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
        // Invalid route values fall back to the search page below.
      }
    }
  }

  window.history.replaceState(null, "", applicationHref("/search"));
  return { page: "search", query: "", typeId: null };
}

export function useAppRoute(): AppRoute {
  const [route, setRoute] = useState<AppRoute>(currentRoute);

  useEffect(() => {
    const changed = () => setRoute(currentRoute());
    window.addEventListener("popstate", changed);
    window.addEventListener(routeChangedEvent, changed);
    return () => {
      window.removeEventListener("popstate", changed);
      window.removeEventListener(routeChangedEvent, changed);
    };
  }, []);

  return route;
}
