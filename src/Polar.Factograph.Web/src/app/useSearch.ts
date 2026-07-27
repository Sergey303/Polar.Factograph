import { useRef, useState } from "react";
import { errorText } from "../api/errorText";
import { factographApi } from "../api/factographApi";
import type { ResourceSearchResult } from "../api/models";

export function useSearch(token: string) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<ResourceSearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const activeRequest = useRef<AbortController | null>(null);

  async function search(): Promise<void> {
    const text = query.trim();
    activeRequest.current?.abort();
    if (text.length === 0) {
      setResults([]);
      setError(null);
      return;
    }

    const controller = new AbortController();
    activeRequest.current = controller;
    setLoading(true);
    setError(null);
    try {
      setResults(await factographApi.search(text, token, controller.signal));
    } catch (reason) {
      if (!controller.signal.aborted) {
        setResults([]);
        setError(errorText(reason));
      }
    } finally {
      if (activeRequest.current === controller) {
        activeRequest.current = null;
        setLoading(false);
      }
    }
  }

  function clear(): void {
    activeRequest.current?.abort();
    setQuery("");
    setResults([]);
    setError(null);
    setLoading(false);
  }

  return {
    query,
    setQuery,
    results,
    loading,
    error,
    search,
    clear
  };
}
