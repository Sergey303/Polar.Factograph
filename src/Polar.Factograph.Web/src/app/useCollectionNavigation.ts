import { useState } from "react";

export function useCollectionNavigation() {
  const [input, setInput] = useState("");
  const [currentId, setCurrentId] = useState<string | null>(null);
  const [history, setHistory] = useState<string[]>([]);

  function open(resourceId?: string): void {
    const next = (resourceId ?? input).trim();
    if (next.length === 0 || next === currentId) {
      return;
    }
    if (currentId !== null) {
      setHistory(values => [...values, currentId]);
    }
    setCurrentId(next);
    setInput(next);
  }

  function back(): void {
    setHistory(values => {
      const previous = values.at(-1);
      if (previous === undefined) {
        return values;
      }
      setCurrentId(previous);
      setInput(previous);
      return values.slice(0, -1);
    });
  }

  function clear(): void {
    setInput("");
    setCurrentId(null);
    setHistory([]);
  }

  return {
    input,
    currentId,
    canGoBack: history.length > 0,
    setInput,
    open,
    back,
    clear
  };
}
