import { useEffect, useRef, useState } from "react";
import { canonicalResourceHref } from "../app/routes";

interface CopyResourceLinkButtonProps {
  resourceId: string;
}

async function copyText(value: string): Promise<void> {
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(value);
    return;
  }

  const previousFocus = document.activeElement instanceof HTMLElement
    ? document.activeElement
    : null;
  const input = document.createElement("textarea");
  input.value = value;
  input.setAttribute("readonly", "");
  input.style.position = "fixed";
  input.style.opacity = "0";
  document.body.appendChild(input);
  input.select();
  const copied = document.execCommand("copy");
  input.remove();
  previousFocus?.focus();
  if (!copied) throw new Error("Copy command was rejected.");
}

export function CopyResourceLinkButton({ resourceId }: CopyResourceLinkButtonProps) {
  const [state, setState] = useState<"idle" | "copied" | "error">("idle");
  const resetTimer = useRef<number | null>(null);

  useEffect(() => () => {
    if (resetTimer.current !== null) window.clearTimeout(resetTimer.current);
  }, []);

  function resetLater(delay: number): void {
    if (resetTimer.current !== null) window.clearTimeout(resetTimer.current);
    resetTimer.current = window.setTimeout(() => {
      resetTimer.current = null;
      setState("idle");
    }, delay);
  }

  async function copy(): Promise<void> {
    try {
      await copyText(canonicalResourceHref(resourceId));
      setState("copied");
      resetLater(1800);
    } catch {
      setState("error");
      resetLater(2600);
    }
  }

  const label = state === "copied"
    ? "Ссылка скопирована"
    : state === "error"
      ? "Не удалось скопировать"
      : "Скопировать ссылку";

  return (
    <button
      className="button ghost compact copy-resource-link"
      type="button"
      onClick={() => void copy()}
      aria-live="polite"
    >
      {label}
    </button>
  );
}
