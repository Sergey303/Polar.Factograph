import { type ReactNode, useEffect, useLayoutEffect, useRef, useState } from "react";

export interface PageVirtualizedChunkDefinition {
  key: string;
  estimatedHeight: number;
  content: ReactNode;
  eager?: boolean;
}

interface PageVirtualizedChunksProps {
  chunks: PageVirtualizedChunkDefinition[];
}

function PageVirtualizedChunk({ definition }: {
  definition: PageVirtualizedChunkDefinition;
}) {
  const elementRef = useRef<HTMLDivElement>(null);
  const [nearViewport, setNearViewport] = useState(definition.eager === true);
  const [measuredHeight, setMeasuredHeight] = useState<number | null>(null);

  useEffect(() => {
    const element = elementRef.current;
    if (element === null) return;
    if (typeof IntersectionObserver === "undefined") {
      setNearViewport(true);
      return;
    }

    const observer = new IntersectionObserver(entries => {
      const next = entries.some(entry => entry.isIntersecting);
      setNearViewport(current => current === next ? current : next);
    }, { rootMargin: "1200px 0px" });
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  useLayoutEffect(() => {
    const element = elementRef.current;
    if (!nearViewport || element === null) return;

    const measure = () => {
      const next = element.getBoundingClientRect().height;
      if (next > 0) {
        setMeasuredHeight(current =>
          current !== null && Math.abs(current - next) < 1 ? current : next);
      }
    };
    measure();

    if (typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(measure);
    observer.observe(element);
    return () => observer.disconnect();
  }, [nearViewport]);

  const placeholderHeight = measuredHeight ?? Math.max(1, definition.estimatedHeight);
  return (
    <div
      ref={elementRef}
      className={`page-virtualized-chunk ${nearViewport ? "active" : "placeholder"}`}
      style={nearViewport ? undefined : { height: `${placeholderHeight}px` }}
    >
      {nearViewport ? definition.content : null}
    </div>
  );
}

export function PageVirtualizedChunks({ chunks }: PageVirtualizedChunksProps) {
  return (
    <div className="page-virtualized-chunks">
      {chunks.map(chunk => (
        <PageVirtualizedChunk key={chunk.key} definition={chunk} />
      ))}
    </div>
  );
}
