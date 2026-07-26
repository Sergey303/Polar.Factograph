import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./app/App";
import "./styles/base.css";
import "./styles/layout.css";
import "./styles/components.css";
import "./styles/portrait.css";
import "./styles/collections.css";
import "./styles/document-actions.css";

const root = document.getElementById("root");
if (root === null) {
  throw new Error("Application root element was not found.");
}

createRoot(root).render(
  <StrictMode>
    <App />
  </StrictMode>
);
