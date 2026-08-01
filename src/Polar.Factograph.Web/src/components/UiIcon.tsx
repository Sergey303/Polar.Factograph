import type { ReactNode } from "react";

interface UiIconProps {
  name:
    | "plus"
    | "file-plus"
    | "relations"
    | "edit"
    | "copy"
    | "check"
    | "warning"
    | "external-link"
    | "replace";
  spinning?: boolean;
}

const paths: Record<UiIconProps["name"], ReactNode> = {
  plus: <path d="M12 5v14M5 12h14" />,
  "file-plus": (
    <>
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z" />
      <path d="M14 2v6h6M12 12v6M9 15h6" />
    </>
  ),
  relations: (
    <>
      <circle cx="6" cy="6" r="3" />
      <circle cx="18" cy="6" r="3" />
      <circle cx="12" cy="18" r="3" />
      <path d="m8.6 7.5 2.2 7M15.4 7.5l-2.2 7M9 6h6" />
    </>
  ),
  edit: (
    <>
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L8 18l-4 1 1-4Z" />
    </>
  ),
  copy: (
    <>
      <rect x="9" y="9" width="12" height="12" rx="2" />
      <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
    </>
  ),
  check: <path d="m5 12 4 4L19 6" />,
  warning: (
    <>
      <path d="M10.3 2.9 1.8 17a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 2.9a2 2 0 0 0-3.4 0Z" />
      <path d="M12 9v4M12 17h.01" />
    </>
  ),
  "external-link": (
    <>
      <path d="M15 3h6v6M10 14 21 3" />
      <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" />
    </>
  ),
  replace: (
    <>
      <path d="M20 7h-7a4 4 0 0 0-4 4v1" />
      <path d="m17 4 3 3-3 3M4 17h7a4 4 0 0 0 4-4v-1" />
      <path d="m7 20-3-3 3-3" />
    </>
  )
};

export function UiIcon({ name, spinning = false }: UiIconProps) {
  return (
    <svg
      className={`button-icon${spinning ? " spinning" : ""}`}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      {paths[name]}
    </svg>
  );
}
