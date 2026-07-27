import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./app/App";
import "./styles/base.css";
import "./styles/layout.css";
import "./styles/components.css";
import "./styles/portrait.css";
import "./styles/collections.css";
import "./styles/document-actions.css";
import "./styles/document-intake.css";
import "./styles/resource-editor.css";
import "./styles/resource-properties.css";
import "./styles/admin-dialog.css";
import "./styles/admin-cards.css";
import "./styles/auth-access.css";

const root = document.getElementById("root");
if (root === null) {
  throw new Error("Application root element was not found.");
}

createRoot(root).render(
  <StrictMode>
    <App />
  </StrictMode>
);
