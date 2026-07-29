import { useState } from "react";
import { canonicalResourceHref } from "../app/routes";

interface CopyResourceLinkButtonProps {
  resourceId: string;
}

async function copyText(value: string): Promise<void> {
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(value);
    return;
  }

  const input = document.createElement("textarea");
  input.value = value;
  input.setAttribute("readonly", "");
  input.style.position = "fixed";
  input.style.opacity = "0";
  document.body.appendChild(input);
  input.select();
  const copied = document.execCommand("copy");
  input.remove();
  if (!copied) throw new Error("Copy command was rejected.");
}

export function CopyResourceLinkButton({ resourceId }: CopyResourceLinkButtonProps) {
  const [state, setState] = useState<"idle" | "copied" | "error">("idle");

  async function copy(): Promise<void> {
    try {
      await copyText(canonicalResourceHref(resourceId));
      setState("copied");
      window.setTimeout(() => setState("idle"), 1800);
    } catch {
      setState("error");
      window.setTimeout(() => setState("idle"), 2600);
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
