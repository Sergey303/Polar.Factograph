import { useEffect } from "react";
import { canonicalResourceHref } from "../app/routes";

interface ResourceDocumentMetadataProps {
  resourceId: string;
  title: string;
  description: string;
  siteName: string;
  imageUrl?: string | null;
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

function removeMeta(
  selectorAttribute: "name" | "property",
  key: string,
  restore: Array<() => void>
): void {
  const selector = `meta[${selectorAttribute}="${key}"]`;
  const existing = document.head.querySelector<HTMLMetaElement>(selector);
  if (existing === null) return;

  const next = existing.nextSibling;
  existing.remove();
  restore.push(() => {
    if (next?.parentNode === document.head) document.head.insertBefore(existing, next);
    else document.head.append(existing);
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
    setMeta("name", "twitter:title", props.title, restore);
    setMeta("name", "twitter:description", props.description, restore);

    if (props.imageUrl) {
      const image = new URL(props.imageUrl, document.baseURI).href;
      setMeta("property", "og:image", image, restore);
      setMeta("property", "og:image:alt", props.title, restore);
      setMeta("name", "twitter:card", "summary_large_image", restore);
      setMeta("name", "twitter:image", image, restore);
      setMeta("name", "twitter:image:alt", props.title, restore);
    } else {
      setMeta("name", "twitter:card", "summary", restore);
      removeMeta("property", "og:image", restore);
      removeMeta("property", "og:image:alt", restore);
      removeMeta("name", "twitter:image", restore);
      removeMeta("name", "twitter:image:alt", restore);
    }

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
  }, [
    props.description,
    props.imageUrl,
    props.resourceId,
    props.siteName,
    props.title
  ]);

  return null;
}
