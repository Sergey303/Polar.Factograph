import { useEffect } from "react";
import { canonicalResourceHref } from "../app/routes";

interface ResourceDocumentMetadataProps {
  resourceId: string;
  title: string;
  description: string;
  siteName: string;
}

function setMeta(
  selectorAttribute: "name" | "property",
  key: string,
  content: string,
  restore: Array<() => void>
): void {
  const selector = `meta[${selectorAttribute}="${key}"]`;
  const existing = document.head.querySelector<HTMLMetaElement>(selector);
  const element = existing ?? document.createElement("meta");
  const previous = existing?.content ?? null;
  if (existing === null) {
    element.setAttribute(selectorAttribute, key);
    document.head.append(element);
  }
  element.content = content;
  restore.push(() => {
    if (existing === null) element.remove();
    else element.content = previous ?? "";
  });
}

export function ResourceDocumentMetadata(props: ResourceDocumentMetadataProps) {
  useEffect(() => {
    const restore: Array<() => void> = [];
    const canonical = canonicalResourceHref(props.resourceId);
    const pageTitle = `${props.title} — ${props.siteName}`;
    const previousTitle = document.title;
    document.title = pageTitle;
    restore.push(() => { document.title = previousTitle; });

    setMeta("name", "description", props.description, restore);
    setMeta("property", "og:title", props.title, restore);
    setMeta("property", "og:description", props.description, restore);
    setMeta("property", "og:type", "website", restore);
    setMeta("property", "og:url", canonical, restore);
    setMeta("property", "og:site_name", props.siteName, restore);
    setMeta("name", "twitter:card", "summary", restore);

    const existingCanonical = document.head.querySelector<HTMLLinkElement>(
      'link[rel="canonical"]'
    );
    const canonicalLink = existingCanonical ?? document.createElement("link");
    const previousHref = existingCanonical?.href ?? null;
    if (existingCanonical === null) {
      canonicalLink.rel = "canonical";
      document.head.append(canonicalLink);
    }
    canonicalLink.href = canonical;
    restore.push(() => {
      if (existingCanonical === null) canonicalLink.remove();
      else canonicalLink.href = previousHref ?? "";
    });

    return () => {
      for (const action of restore.reverse()) action();
    };
  }, [props.description, props.resourceId, props.siteName, props.title]);

  return null;
}
